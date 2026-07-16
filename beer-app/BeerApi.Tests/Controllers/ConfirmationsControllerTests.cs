using System.Security.Claims;
using BeerApi.Controllers;
using BeerApi.Data;
using BeerApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BeerApi.Tests.Controllers;

public class ConfirmationsControllerTests
{
    private const string CustomerId = "customer-1";
    private const string BartenderId = "bartender-1";
    private const string BartenderPin = "123456";

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<Beer> SeedWorldAsync(ApplicationDbContext context, bool bartenderPinActive = true, string bartenderRole = "Bartender")
    {
        var beer = new Beer { Name = "Duvel", Brewery = "Duvel Moortgat", Style = "Belgian Strong Golden Ale" };
        context.Beers.Add(beer);
        context.Taverns.Add(new Tavern { Name = "The Tavern" });

        var role = new IdentityRole(bartenderRole) { Id = $"role-{bartenderRole}", NormalizedName = bartenderRole.ToUpperInvariant() };
        context.Roles.Add(role);
        context.Users.Add(new IdentityUser { Id = BartenderId, UserName = "bartender@example.com" });
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = BartenderId, RoleId = role.Id });

        var hasher = new PasswordHasher<IdentityUser>();
        context.StaffPins.Add(new StaffPin
        {
            UserId = BartenderId,
            PinHash = hasher.HashPassword(new IdentityUser(), BartenderPin),
            IsActive = bartenderPinActive,
        });

        await context.SaveChangesAsync();
        return beer;
    }

    private static ConfirmationsController CreateController(ApplicationDbContext context, string userId = CustomerId)
    {
        return new ConfirmationsController(context)
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

    [Fact]
    public async Task PostConfirmation_WithValidPin_CreatesConfirmation_AttributedToBartender()
    {
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context);
        var controller = CreateController(context);

        var result = await controller.PostConfirmation(new ConfirmationRequest(beer.Id, BartenderPin));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var response = Assert.IsType<ConfirmationResponse>(created.Value);
        Assert.Equal(1, response.ConfirmedCount);
        Assert.Equal(ConfirmationsController.MugGoal, response.Goal);
        Assert.False(response.MugEarned);
        Assert.Equal(beer.Name, response.BeerName);

        var confirmation = Assert.Single(context.BeerConfirmations);
        Assert.Equal(CustomerId, confirmation.CustomerId);
        Assert.Equal(BartenderId, confirmation.ConfirmedByUserId);
    }

    [Fact]
    public async Task PostConfirmation_SameBeerTwice_ReturnsConflict()
    {
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context);
        var controller = CreateController(context);
        await controller.PostConfirmation(new ConfirmationRequest(beer.Id, BartenderPin));

        var result = await controller.PostConfirmation(new ConfirmationRequest(beer.Id, BartenderPin));

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(context.BeerConfirmations);
    }

    [Fact]
    public async Task PostConfirmation_UnknownBeer_ReturnsNotFound()
    {
        using var context = CreateContext();
        await SeedWorldAsync(context);
        var controller = CreateController(context);

        var result = await controller.PostConfirmation(new ConfirmationRequest(999, BartenderPin));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12ab56")]
    [InlineData("1234567")]
    public async Task PostConfirmation_MalformedPin_ReturnsBadRequest(string pin)
    {
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context);
        var controller = CreateController(context);

        var result = await controller.PostConfirmation(new ConfirmationRequest(beer.Id, pin));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.BeerConfirmations);
    }

    [Fact]
    public async Task PostConfirmation_WrongPin_ReturnsUnauthorized_AndSavesNothing()
    {
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context);
        var controller = CreateController(context);

        var result = await controller.PostConfirmation(new ConfirmationRequest(beer.Id, "654321"));

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Empty(context.BeerConfirmations);
    }

    [Fact]
    public async Task PostConfirmation_InactivePin_ReturnsUnauthorized()
    {
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context, bartenderPinActive: false);
        var controller = CreateController(context);

        var result = await controller.PostConfirmation(new ConfirmationRequest(beer.Id, BartenderPin));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task PostConfirmation_PinOwnerWithoutStaffRole_ReturnsUnauthorized()
    {
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context, bartenderRole: "Customer");
        var controller = CreateController(context);

        var result = await controller.PostConfirmation(new ConfirmationRequest(beer.Id, BartenderPin));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task PostConfirmation_ReachingGoal_SetsMugEarned()
    {
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context);
        for (var i = 0; i < ConfirmationsController.MugGoal - 1; i++)
        {
            context.BeerConfirmations.Add(new BeerConfirmation
            {
                CustomerId = CustomerId,
                BeerId = 1000 + i,
                TavernId = 1,
                ConfirmedByUserId = BartenderId,
            });
        }
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.PostConfirmation(new ConfirmationRequest(beer.Id, BartenderPin));

        var created = Assert.IsType<ObjectResult>(result);
        var response = Assert.IsType<ConfirmationResponse>(created.Value);
        Assert.Equal(ConfirmationsController.MugGoal, response.ConfirmedCount);
        Assert.True(response.MugEarned);
    }

    // --- #12 PIN lockout ---

    [Fact]
    public async Task PostConfirmation_AfterMaxWrongPins_LocksPin_EvenCorrectPinIsRejected()
    {
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context);
        var guesser = CreateController(context);
        for (var i = 0; i < ConfirmationsController.MaxPinFailures; i++)
        {
            await guesser.PostConfirmation(new ConfirmationRequest(beer.Id, "000000"));
        }

        // A different customer, so only the per-PIN axis can be what rejects here.
        var otherCustomer = CreateController(context, userId: "customer-2");
        var result = await otherCustomer.PostConfirmation(new ConfirmationRequest(beer.Id, BartenderPin));

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Empty(context.BeerConfirmations);
        var pin = Assert.Single(context.StaffPins);
        Assert.Equal(ConfirmationsController.MaxPinFailures, pin.FailedAttempts);
        Assert.NotNull(pin.LockedUntil);
    }

    [Fact]
    public async Task PostConfirmation_LockedPinRejection_LooksIdenticalToWrongPin()
    {
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context);
        var controller = CreateController(context);
        var wrongPin = await controller.PostConfirmation(new ConfirmationRequest(beer.Id, "000000"));

        var pin = Assert.Single(context.StaffPins);
        pin.FailedAttempts = ConfirmationsController.MaxPinFailures;
        pin.LockedUntil = DateTime.UtcNow.AddMinutes(10);
        await context.SaveChangesAsync();

        var lockedCorrectPin = await controller.PostConfirmation(new ConfirmationRequest(beer.Id, BartenderPin));

        // No lockout oracle: same status, same body as a plain wrong guess.
        var wrongBody = Assert.IsType<UnauthorizedObjectResult>(wrongPin).Value!.ToString();
        var lockedBody = Assert.IsType<UnauthorizedObjectResult>(lockedCorrectPin).Value!.ToString();
        Assert.Equal(wrongBody, lockedBody);
    }

    [Fact]
    public async Task PostConfirmation_ExpiredLock_AcceptsCorrectPin_AndResetsCounters()
    {
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context);
        var pin = Assert.Single(context.StaffPins);
        pin.FailedAttempts = ConfirmationsController.MaxPinFailures;
        pin.LockedUntil = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.PostConfirmation(new ConfirmationRequest(beer.Id, BartenderPin));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal(0, pin.FailedAttempts);
        Assert.Null(pin.LockedUntil);
    }

    [Fact]
    public async Task PostConfirmation_SuccessfulConfirm_ResetsPinCounter_AndCustomerWindow()
    {
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context);
        var controller = CreateController(context);
        for (var i = 0; i < ConfirmationsController.MaxPinFailures - 1; i++)
        {
            await controller.PostConfirmation(new ConfirmationRequest(beer.Id, "000000"));
        }

        var result = await controller.PostConfirmation(new ConfirmationRequest(beer.Id, BartenderPin));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var pin = Assert.Single(context.StaffPins);
        Assert.Equal(0, pin.FailedAttempts);
        Assert.Null(pin.LockedUntil);
        Assert.Empty(context.FailedConfirmationAttempts.Where(a => a.CustomerId == CustomerId));
    }

    [Fact]
    public async Task PostConfirmation_CustomerExceedingFailureWindow_IsBlockedOnTheirOwnAxis()
    {
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context);
        var abuser = CreateController(context);
        for (var i = 0; i < ConfirmationsController.MaxCustomerFailures; i++)
        {
            await abuser.PostConfirmation(new ConfirmationRequest(beer.Id, "000000"));
        }

        // Clear the per-PIN axis so only the customer's own record can reject them —
        // this is the account-level block that spreading guesses across PINs can't dodge.
        var pin = Assert.Single(context.StaffPins);
        pin.FailedAttempts = 0;
        pin.LockedUntil = null;
        await context.SaveChangesAsync();

        var blocked = await abuser.PostConfirmation(new ConfirmationRequest(beer.Id, BartenderPin));
        Assert.IsType<UnauthorizedObjectResult>(blocked);

        // An innocent customer confirms with the same PIN, same moment: only the abuser is blocked.
        var innocent = CreateController(context, userId: "customer-2");
        var ok = await innocent.PostConfirmation(new ConfirmationRequest(beer.Id, BartenderPin));
        var created = Assert.IsType<ObjectResult>(ok);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
    }
}
