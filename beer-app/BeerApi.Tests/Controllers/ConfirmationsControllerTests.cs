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

    private static async Task<Beer> SeedWorldAsync(ApplicationDbContext context, bool bartenderPinActive = true, string bartenderRole = "Bartender", string bartenderPin = BartenderPin)
    {
        var beer = new Beer { Name = "Duvel", Brewery = "Duvel Moortgat", Style = "Belgian Strong Golden Ale" };
        context.Beers.Add(beer);
        context.Taverns.Add(new Tavern { Name = "The Tavern" });

        var role = new IdentityRole(bartenderRole) { Id = $"role-{bartenderRole}", NormalizedName = bartenderRole.ToUpperInvariant() };
        context.Roles.Add(role);
        context.Users.Add(new ApplicationUser { Id = BartenderId, UserName = "bartender@example.com" });
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = BartenderId, RoleId = role.Id });

        var hasher = new PasswordHasher<ApplicationUser>();
        context.StaffPins.Add(new StaffPin
        {
            UserId = BartenderId,
            PinHash = hasher.HashPassword(new ApplicationUser(), bartenderPin),
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
        Assert.False(response.MilestoneReached);
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
    [InlineData("123456789")]
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
    public async Task PostConfirmation_WithEightDigitBartenderPin_Succeeds()
    {
        // #79: an admin can issue a longer, memorable PIN (e.g. an 8-digit birthday)
        // instead of a random 6-digit one — the confirm path must accept it identically.
        const string eightDigitPin = "07041999";
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context, bartenderPin: eightDigitPin);
        var controller = CreateController(context);

        var result = await controller.PostConfirmation(new ConfirmationRequest(beer.Id, eightDigitPin));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
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

    // --- #74 lightweight milestone moment (distinct from the durable mug award) ---

    [Fact]
    public async Task PostConfirmation_ReachingMilestoneCount_SetsMilestoneReached()
    {
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context);
        for (var i = 0; i < ConfirmationsController.MilestoneCount - 1; i++)
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
        Assert.Equal(ConfirmationsController.MilestoneCount, response.ConfirmedCount);
        Assert.True(response.MilestoneReached);
        Assert.False(response.MugEarned);
    }

    [Fact]
    public async Task PostConfirmation_OneBelowMilestoneCount_DoesNotSetMilestoneReached()
    {
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context);
        for (var i = 0; i < ConfirmationsController.MilestoneCount - 2; i++)
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
        Assert.Equal(ConfirmationsController.MilestoneCount - 1, response.ConfirmedCount);
        Assert.False(response.MilestoneReached);
    }

    // --- #14 durable mug award ---

    [Fact]
    public async Task PostConfirmation_ReachingGoal_StampsMugAwardExactlyOnce()
    {
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context);
        var extraBeer = new Beer { Name = "Orval", Brewery = "Brasserie d'Orval", Style = "Belgian Pale Ale" };
        context.Beers.Add(extraBeer);
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

        await controller.PostConfirmation(new ConfirmationRequest(beer.Id, BartenderPin));

        var award = Assert.Single(context.MugAwards);
        Assert.Equal(CustomerId, award.CustomerId);
        var stampedAt = award.EarnedAt;

        // Beer 201 must not re-stamp the milestone.
        await controller.PostConfirmation(new ConfirmationRequest(extraBeer.Id, BartenderPin));

        award = Assert.Single(context.MugAwards);
        Assert.Equal(stampedAt, award.EarnedAt);
    }

    [Fact]
    public async Task PostConfirmation_BelowGoal_StampsNoAward()
    {
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context);
        var controller = CreateController(context);

        await controller.PostConfirmation(new ConfirmationRequest(beer.Id, BartenderPin));

        Assert.Empty(context.MugAwards);
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

    [Fact]
    public async Task SetBeerAvailabilityViaPin_ValidPin_MarksOutOfStock_AttributedToBartender()
    {
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context);
        beer.Availability = BeerAvailability.OnTap;
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.SetBeerAvailabilityViaPin(
            new PinAvailabilityRequest(beer.Id, BartenderPin, BeerAvailability.OutOfStock));

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(BeerAvailability.OutOfStock, (await context.Beers.FindAsync(beer.Id))!.Availability);

        var audit = Assert.Single(context.AdminAudits);
        Assert.Equal(BartenderId, audit.AdminUserId);
        Assert.Equal("Beer", audit.EntityType);
        Assert.Equal("AvailabilityChange", audit.Action);
        Assert.Equal("OnTap", audit.BeforeSnapshot);
        Assert.Equal("OutOfStock", audit.AfterSnapshot);
    }

    [Fact]
    public async Task SetBeerAvailabilityViaPin_ValidPin_MarksAvailableAgain()
    {
        // #80's "both directions" requirement: flipping back to available works the
        // same way as flipping to out-of-stock.
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context);
        beer.Availability = BeerAvailability.OutOfStock;
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.SetBeerAvailabilityViaPin(
            new PinAvailabilityRequest(beer.Id, BartenderPin, BeerAvailability.Available));

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(BeerAvailability.Available, (await context.Beers.FindAsync(beer.Id))!.Availability);
    }

    [Theory]
    [InlineData(BeerAvailability.OnTap)]
    [InlineData(BeerAvailability.Retired)]
    public async Task SetBeerAvailabilityViaPin_DisallowedTargetState_ReturnsBadRequest_AndChangesNothing(BeerAvailability target)
    {
        // Narrower than BeersController.UpdateAvailability (Admin-only, all 4 states) —
        // this PIN-gated path only allows the two states a bartender plausibly needs.
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context);
        beer.Availability = BeerAvailability.Available;
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.SetBeerAvailabilityViaPin(new PinAvailabilityRequest(beer.Id, BartenderPin, target));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(BeerAvailability.Available, (await context.Beers.FindAsync(beer.Id))!.Availability);
        Assert.Empty(context.AdminAudits);
    }

    [Fact]
    public async Task SetBeerAvailabilityViaPin_AlreadyAtTargetState_IsNoOp_WritesNoAudit()
    {
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context);
        beer.Availability = BeerAvailability.OutOfStock;
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.SetBeerAvailabilityViaPin(
            new PinAvailabilityRequest(beer.Id, BartenderPin, BeerAvailability.OutOfStock));

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(context.AdminAudits);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12ab56")]
    [InlineData("123456789")]
    public async Task SetBeerAvailabilityViaPin_MalformedPin_ReturnsBadRequest(string pin)
    {
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context);
        var controller = CreateController(context);

        var result = await controller.SetBeerAvailabilityViaPin(
            new PinAvailabilityRequest(beer.Id, pin, BeerAvailability.OutOfStock));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SetBeerAvailabilityViaPin_WrongPin_ReturnsUnauthorized_SameGenericMessageAsConfirmation()
    {
        using var context = CreateContext();
        var beer = await SeedWorldAsync(context);
        var controller = CreateController(context);

        var result = await controller.SetBeerAvailabilityViaPin(
            new PinAvailabilityRequest(beer.Id, "000000", BeerAvailability.OutOfStock));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(BeerAvailability.Available, (await context.Beers.FindAsync(beer.Id))!.Availability);
        Assert.Empty(context.AdminAudits);

        // Same rejection body a wrong confirmation PIN gets — indistinguishable to the client.
        var confirmResult = await controller.PostConfirmation(new ConfirmationRequest(beer.Id, "000000"));
        var confirmUnauthorized = Assert.IsType<UnauthorizedObjectResult>(confirmResult);
        Assert.Equal(
            System.Text.Json.JsonSerializer.Serialize(unauthorized.Value),
            System.Text.Json.JsonSerializer.Serialize(confirmUnauthorized.Value));
    }

    [Fact]
    public async Task SetBeerAvailabilityViaPin_UnknownBeer_ReturnsNotFound()
    {
        using var context = CreateContext();
        await SeedWorldAsync(context);
        var controller = CreateController(context);

        var result = await controller.SetBeerAvailabilityViaPin(
            new PinAvailabilityRequest(999, BartenderPin, BeerAvailability.OutOfStock));

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
