using BeerApi.Controllers;
using BeerApi.Data;
using BeerApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace BeerApi.Tests.Controllers;

// Role gating ([Authorize(Roles = ...)]) is middleware behavior covered by the HTTP-level
// StaffPinLifecycleTests; these tests cover the controller's own logic.
public class StaffPinsControllerTests
{
    private const string BartenderId = "bartender-1";
    private const string OtherBartenderId = "bartender-2";
    private const string CustomerId = "customer-1";

    private static readonly PasswordHasher<ApplicationUser> Hasher = new();

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task SeedStaffAsync(ApplicationDbContext context)
    {
        var bartenderRole = new IdentityRole("Bartender") { Id = "role-bartender", NormalizedName = "BARTENDER" };
        var customerRole = new IdentityRole("Customer") { Id = "role-customer", NormalizedName = "CUSTOMER" };
        context.Roles.AddRange(bartenderRole, customerRole);
        context.Users.AddRange(
            new ApplicationUser { Id = BartenderId, UserName = "b1@example.com" },
            new ApplicationUser { Id = OtherBartenderId, UserName = "b2@example.com" },
            new ApplicationUser { Id = CustomerId, UserName = "c1@example.com" });
        context.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = BartenderId, RoleId = bartenderRole.Id },
            new IdentityUserRole<string> { UserId = OtherBartenderId, RoleId = bartenderRole.Id },
            new IdentityUserRole<string> { UserId = CustomerId, RoleId = customerRole.Id });
        await context.SaveChangesAsync();
    }

    private static StaffPinsController CreateController(ApplicationDbContext context, string userId = BartenderId)
    {
        return new StaffPinsController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test")),
                },
            },
        };
    }

    private static StaffPin AddPin(ApplicationDbContext context, string userId, string pin, bool isActive = true)
    {
        var staffPin = new StaffPin
        {
            UserId = userId,
            PinHash = Hasher.HashPassword(new ApplicationUser(), pin),
            IsActive = isActive,
        };
        context.StaffPins.Add(staffPin);
        context.SaveChanges();
        return staffPin;
    }

    private static bool PinVerifies(StaffPin staffPin, string pin) =>
        Hasher.VerifyHashedPassword(new ApplicationUser(), staffPin.PinHash, pin) != PasswordVerificationResult.Failed;

    [Fact]
    public async Task SetMyPin_FirstTime_CreatesActiveHashedPin()
    {
        using var context = CreateContext();
        await SeedStaffAsync(context);
        var controller = CreateController(context);

        var result = await controller.SetMyPin(new SetPinRequest("654321"));

        Assert.IsType<NoContentResult>(result);
        var pin = Assert.Single(context.StaffPins);
        Assert.Equal(BartenderId, pin.UserId);
        Assert.True(pin.IsActive);
        Assert.NotEqual("654321", pin.PinHash);
        Assert.True(PinVerifies(pin, "654321"));
    }

    [Fact]
    public async Task SetMyPin_ChangesExistingPin_AndClearsLockState()
    {
        using var context = CreateContext();
        await SeedStaffAsync(context);
        var pin = AddPin(context, BartenderId, "111111");
        pin.FailedAttempts = 3;
        pin.LockedUntil = DateTime.UtcNow.AddMinutes(10);
        pin.IsActive = false;
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.SetMyPin(new SetPinRequest("654321"));

        Assert.IsType<NoContentResult>(result);
        var updated = Assert.Single(context.StaffPins);
        Assert.True(updated.IsActive);
        Assert.Equal(0, updated.FailedAttempts);
        Assert.Null(updated.LockedUntil);
        Assert.False(PinVerifies(updated, "111111"));
        Assert.True(PinVerifies(updated, "654321"));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12ab56")]
    [InlineData("1234567")]
    [InlineData("")]
    public async Task SetMyPin_MalformedPin_ReturnsBadRequest(string pin)
    {
        using var context = CreateContext();
        await SeedStaffAsync(context);
        var controller = CreateController(context);

        var result = await controller.SetMyPin(new SetPinRequest(pin));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.StaffPins);
    }

    [Fact]
    public async Task SetMyPin_CollidingWithAnotherActivePin_ReturnsConflict()
    {
        using var context = CreateContext();
        await SeedStaffAsync(context);
        AddPin(context, OtherBartenderId, "654321");
        var controller = CreateController(context);

        var result = await controller.SetMyPin(new SetPinRequest("654321"));

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(context.StaffPins);
    }

    [Fact]
    public async Task SetMyPin_MatchingOnlyAnInactivePin_Succeeds()
    {
        using var context = CreateContext();
        await SeedStaffAsync(context);
        AddPin(context, OtherBartenderId, "654321", isActive: false);
        var controller = CreateController(context);

        var result = await controller.SetMyPin(new SetPinRequest("654321"));

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SetMyPin_ReusingOwnCurrentPin_IsNotACollision()
    {
        using var context = CreateContext();
        await SeedStaffAsync(context);
        AddPin(context, BartenderId, "654321");
        var controller = CreateController(context);

        var result = await controller.SetMyPin(new SetPinRequest("654321"));

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task IssuePin_ForStaffUser_CreatesActivePin()
    {
        using var context = CreateContext();
        await SeedStaffAsync(context);
        var controller = CreateController(context);

        var result = await controller.IssuePin(OtherBartenderId, new SetPinRequest("222222"));

        Assert.IsType<NoContentResult>(result);
        var pin = Assert.Single(context.StaffPins);
        Assert.Equal(OtherBartenderId, pin.UserId);
        Assert.True(pin.IsActive);
        Assert.True(PinVerifies(pin, "222222"));
    }

    [Fact]
    public async Task IssuePin_ForNonStaffUser_ReturnsBadRequest()
    {
        using var context = CreateContext();
        await SeedStaffAsync(context);
        var controller = CreateController(context);

        var result = await controller.IssuePin(CustomerId, new SetPinRequest("222222"));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.StaffPins);
    }

    [Fact]
    public async Task IssuePin_ForUnknownUser_ReturnsNotFound()
    {
        using var context = CreateContext();
        await SeedStaffAsync(context);
        var controller = CreateController(context);

        var result = await controller.IssuePin("nobody", new SetPinRequest("222222"));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeactivatePin_SetsInactive()
    {
        using var context = CreateContext();
        await SeedStaffAsync(context);
        AddPin(context, OtherBartenderId, "222222");
        var controller = CreateController(context);

        var result = await controller.DeactivatePin(OtherBartenderId);

        Assert.IsType<NoContentResult>(result);
        Assert.False(Assert.Single(context.StaffPins).IsActive);
    }

    [Fact]
    public async Task DeactivatePin_WithoutExistingPin_ReturnsNotFound()
    {
        using var context = CreateContext();
        await SeedStaffAsync(context);
        var controller = CreateController(context);

        var result = await controller.DeactivatePin(OtherBartenderId);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
