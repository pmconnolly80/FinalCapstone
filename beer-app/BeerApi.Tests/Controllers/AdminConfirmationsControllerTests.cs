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

// Role gating is middleware behavior covered by the HTTP-level AdminConfirmationsTests;
// these tests cover the listing, filtering, and void/audit logic.
public class AdminConfirmationsControllerTests
{
    private const string AdminId = "admin-1";
    private const string CustomerId = "customer-1";
    private const string OtherCustomerId = "customer-2";
    private const string BartenderId = "bartender-1";

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed record World(Beer Duvel, Beer Orval, BeerConfirmation OlderDuvel, BeerConfirmation NewerOrval);

    private static async Task<World> SeedWorldAsync(ApplicationDbContext context)
    {
        var duvel = new Beer { Name = "Duvel", Brewery = "Duvel Moortgat", Style = "Belgian Strong Golden Ale" };
        var orval = new Beer { Name = "Orval", Brewery = "Brasserie d'Orval", Style = "Belgian Pale Ale" };
        context.Beers.AddRange(duvel, orval);
        context.Taverns.Add(new Tavern { Name = "The Tavern" });
        context.Users.AddRange(
            new IdentityUser { Id = AdminId, Email = "admin@example.com", UserName = "admin@example.com" },
            new IdentityUser { Id = CustomerId, Email = "customer@example.com", UserName = "customer@example.com" },
            new IdentityUser { Id = OtherCustomerId, Email = "other@example.com", UserName = "other@example.com" },
            new IdentityUser { Id = BartenderId, Email = "bartender@example.com", UserName = "bartender@example.com" });
        await context.SaveChangesAsync();

        var older = new BeerConfirmation
        {
            CustomerId = CustomerId,
            BeerId = duvel.Id,
            TavernId = 1,
            ConfirmedByUserId = BartenderId,
            ConfirmedAt = new DateTime(2026, 7, 1, 20, 0, 0, DateTimeKind.Utc),
        };
        var newer = new BeerConfirmation
        {
            CustomerId = CustomerId,
            BeerId = orval.Id,
            TavernId = 1,
            ConfirmedByUserId = BartenderId,
            ConfirmedAt = new DateTime(2026, 7, 15, 21, 0, 0, DateTimeKind.Utc),
        };
        context.BeerConfirmations.AddRange(older, newer);
        await context.SaveChangesAsync();
        return new World(duvel, orval, older, newer);
    }

    private static AdminConfirmationsController CreateController(ApplicationDbContext context, string userId = AdminId)
    {
        return new AdminConfirmationsController(context)
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
    public async Task GetConfirmations_ListsAllWithEmailsAndBeerNames()
    {
        using var context = CreateContext();
        await SeedWorldAsync(context);
        var controller = CreateController(context);

        var result = await controller.GetConfirmations(null, null, null, null, null);

        var rows = Assert.IsAssignableFrom<IReadOnlyList<AdminConfirmationResponse>>(result.Value);
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, r => r.BeerName == "Duvel" && r.CustomerEmail == "customer@example.com"
            && r.BartenderEmail == "bartender@example.com");
    }

    [Fact]
    public async Task GetConfirmations_FiltersByCustomerBeerAndDateRange()
    {
        using var context = CreateContext();
        var world = await SeedWorldAsync(context);
        context.BeerConfirmations.Add(new BeerConfirmation
        {
            CustomerId = OtherCustomerId,
            BeerId = world.Duvel.Id,
            TavernId = 1,
            ConfirmedByUserId = BartenderId,
            ConfirmedAt = new DateTime(2026, 7, 10, 19, 0, 0, DateTimeKind.Utc),
        });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var byCustomer = await controller.GetConfirmations(OtherCustomerId, null, null, null, null);
        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<AdminConfirmationResponse>>(byCustomer.Value));

        var byBeer = await controller.GetConfirmations(null, null, world.Orval.Id, null, null);
        var beerRows = Assert.IsAssignableFrom<IReadOnlyList<AdminConfirmationResponse>>(byBeer.Value);
        Assert.Equal("Orval", Assert.Single(beerRows).BeerName);

        var byRange = await controller.GetConfirmations(null, null, null,
            new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc));
        var rangeRows = Assert.IsAssignableFrom<IReadOnlyList<AdminConfirmationResponse>>(byRange.Value);
        Assert.Equal("other@example.com", Assert.Single(rangeRows).CustomerEmail);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Void_WithoutReason_ReturnsBadRequest_AndChangesNothing(string? reason)
    {
        using var context = CreateContext();
        var world = await SeedWorldAsync(context);
        var controller = CreateController(context);

        var result = await controller.VoidConfirmation(world.OlderDuvel.Id, new VoidConfirmationRequest(reason!));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(2, context.BeerConfirmations.Count());
        Assert.Empty(context.ConfirmationAudits);
    }

    [Fact]
    public async Task Void_UnknownId_ReturnsNotFound()
    {
        using var context = CreateContext();
        await SeedWorldAsync(context);
        var controller = CreateController(context);

        var result = await controller.VoidConfirmation(999, new VoidConfirmationRequest("wrong beer tapped"));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Void_RemovesConfirmation_AndWritesAuditWithOriginalData()
    {
        using var context = CreateContext();
        var world = await SeedWorldAsync(context);
        var controller = CreateController(context);

        var result = await controller.VoidConfirmation(world.OlderDuvel.Id, new VoidConfirmationRequest("wrong beer tapped"));

        Assert.IsType<NoContentResult>(result);
        Assert.DoesNotContain(context.BeerConfirmations, c => c.Id == world.OlderDuvel.Id);
        var audit = Assert.Single(context.ConfirmationAudits);
        Assert.Equal(AdminId, audit.AdminUserId);
        Assert.Equal("wrong beer tapped", audit.Reason);
        Assert.Equal(world.OlderDuvel.Id, audit.OriginalConfirmationId);
        Assert.Equal(CustomerId, audit.CustomerId);
        Assert.Equal(world.Duvel.Id, audit.BeerId);
        Assert.Equal("Duvel", audit.BeerName);
        Assert.Equal(BartenderId, audit.ConfirmedByUserId);
        Assert.Equal(world.OlderDuvel.ConfirmedAt, audit.ConfirmedAt);
        Assert.True((DateTime.UtcNow - audit.CorrectedAt).Duration() < TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Void_LeavesMugAwardIntact_EarnedIsPermanent()
    {
        using var context = CreateContext();
        var world = await SeedWorldAsync(context);
        context.MugAwards.Add(new MugAward { CustomerId = CustomerId, TavernId = 1 });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        await controller.VoidConfirmation(world.OlderDuvel.Id, new VoidConfirmationRequest("count correction"));
        await controller.VoidConfirmation(world.NewerOrval.Id, new VoidConfirmationRequest("count correction"));

        Assert.Empty(context.BeerConfirmations);
        Assert.Single(context.MugAwards);
    }

    [Fact]
    public async Task GetAudits_NewestCorrectionFirst_WithEmails()
    {
        using var context = CreateContext();
        var world = await SeedWorldAsync(context);
        var controller = CreateController(context);
        await controller.VoidConfirmation(world.OlderDuvel.Id, new VoidConfirmationRequest("first correction"));
        await controller.VoidConfirmation(world.NewerOrval.Id, new VoidConfirmationRequest("second correction"));

        var result = await controller.GetAudits();

        var audits = Assert.IsAssignableFrom<IReadOnlyList<ConfirmationAuditResponse>>(result.Value);
        Assert.Equal(2, audits.Count);
        Assert.Equal("second correction", audits[0].Reason);
        Assert.Equal("admin@example.com", audits[0].AdminEmail);
        Assert.Equal("customer@example.com", audits[0].CustomerEmail);
        Assert.Equal("Orval", audits[0].BeerName);
    }
}
