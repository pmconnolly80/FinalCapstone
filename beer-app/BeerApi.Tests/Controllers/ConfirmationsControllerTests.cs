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
}
