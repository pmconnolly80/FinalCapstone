using System.Security.Claims;
using BeerApi.Controllers;
using BeerApi.Data;
using BeerApi.Models;
using BeerApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BeerApi.Tests.Controllers;

public class BeersControllerTests
{
    private const string CustomerId = "customer-1";

    private class FakeBreweryLookupService : IBreweryLookupService
    {
        public BreweryInfo? Result { get; set; }
        public string? LastRequestedId { get; private set; }

        public Task<BreweryInfo?> GetBreweryAsync(string breweryId)
        {
            LastRequestedId = breweryId;
            return Task.FromResult(Result);
        }

        public Task<IReadOnlyList<BreweryInfo>> SearchBreweriesAsync(string query) =>
            Task.FromResult<IReadOnlyList<BreweryInfo>>(Array.Empty<BreweryInfo>());
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static BeersController CreateController(
        ApplicationDbContext context, string? userId = null, IBreweryLookupService? breweryLookup = null)
    {
        var identity = userId == null
            ? new ClaimsIdentity()
            : new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test");

        return new BeersController(context, breweryLookup ?? new FakeBreweryLookupService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
            },
        };
    }

    [Fact]
    public async Task GetBeers_ReturnsSeededBeers_OrderedByName()
    {
        using var context = CreateContext();
        context.Beers.AddRange(
            new Beer { Name = "Zythos", Brewery = "Brewery Z", Style = "IPA" },
            new Beer { Name = "Ale", Brewery = "Brewery A", Style = "Pale Ale" });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.GetBeers();

        var response = Assert.IsType<BeerSearchResponse>(result.Value);
        Assert.Equal(new[] { "Ale", "Zythos" }, response.Items.Select(b => b.Name));
        Assert.Equal(2, response.TotalCount);
    }

    [Fact]
    public async Task GetBeers_WithNoAvailabilityFilter_DefaultsToInStock()
    {
        using var context = CreateContext();
        context.Beers.AddRange(
            new Beer { Name = "On Tap Beer", Brewery = "B", Style = "S", Availability = BeerAvailability.OnTap },
            new Beer { Name = "Available Beer", Brewery = "B", Style = "S", Availability = BeerAvailability.Available },
            new Beer { Name = "Out Beer", Brewery = "B", Style = "S", Availability = BeerAvailability.OutOfStock },
            new Beer { Name = "Retired Beer", Brewery = "B", Style = "S", Availability = BeerAvailability.Retired });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.GetBeers();

        var response = Assert.IsType<BeerSearchResponse>(result.Value);
        Assert.Equal(new[] { "Available Beer", "On Tap Beer" }, response.Items.Select(b => b.Name).OrderBy(n => n));
    }

    [Fact]
    public async Task GetBeers_WithAvailabilityAll_IncludesEveryState()
    {
        using var context = CreateContext();
        context.Beers.AddRange(
            new Beer { Name = "Available Beer", Brewery = "B", Style = "S", Availability = BeerAvailability.Available },
            new Beer { Name = "Retired Beer", Brewery = "B", Style = "S", Availability = BeerAvailability.Retired });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.GetBeers(availability: "all");

        var response = Assert.IsType<BeerSearchResponse>(result.Value);
        Assert.Equal(2, response.TotalCount);
    }

    [Fact]
    public async Task GetBeers_WithSpecificAvailability_FiltersToThatState()
    {
        using var context = CreateContext();
        context.Beers.AddRange(
            new Beer { Name = "Available Beer", Brewery = "B", Style = "S", Availability = BeerAvailability.Available },
            new Beer { Name = "Out Beer", Brewery = "B", Style = "S", Availability = BeerAvailability.OutOfStock });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.GetBeers(availability: "OutOfStock");

        var response = Assert.IsType<BeerSearchResponse>(result.Value);
        Assert.Equal(new[] { "Out Beer" }, response.Items.Select(b => b.Name));
    }

    [Fact]
    public async Task GetBeers_WithUnknownAvailability_ReturnsBadRequest()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetBeers(availability: "sparkling");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetBeers_WithSearchTerm_MatchesNameBreweryOrStyle_CaseInsensitive()
    {
        using var context = CreateContext();
        context.Beers.AddRange(
            new Beer { Name = "Duvel", Brewery = "Duvel Moortgat", Style = "Belgian Strong Golden Ale" },
            new Beer { Name = "60 Minute IPA", Brewery = "Dogfish Head", Style = "IPA" },
            new Beer { Name = "Guinness Draught", Brewery = "Guinness", Style = "Irish Dry Stout" });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var byName = await controller.GetBeers(search: "duv");
        var byBrewery = await controller.GetBeers(search: "DOGFISH");
        var byStyle = await controller.GetBeers(search: "stout");

        Assert.Equal(new[] { "Duvel" }, Assert.IsType<BeerSearchResponse>(byName.Value).Items.Select(b => b.Name));
        Assert.Equal(new[] { "60 Minute IPA" }, Assert.IsType<BeerSearchResponse>(byBrewery.Value).Items.Select(b => b.Name));
        Assert.Equal(new[] { "Guinness Draught" }, Assert.IsType<BeerSearchResponse>(byStyle.Value).Items.Select(b => b.Name));
    }

    [Fact]
    public async Task GetBeers_WithHadStatusAndNoAuthenticatedUser_ReturnsUnauthorized()
    {
        using var context = CreateContext();
        var controller = CreateController(context, userId: null);

        var result = await controller.GetBeers(hadStatus: "had");

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task GetBeers_WithUnknownHadStatus_ReturnsBadRequest()
    {
        using var context = CreateContext();
        var controller = CreateController(context, userId: CustomerId);

        var result = await controller.GetBeers(hadStatus: "maybe");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetBeers_WithHadStatusHad_ReturnsOnlyConfirmedBeers()
    {
        using var context = CreateContext();
        var had = new Beer { Name = "Had Beer", Brewery = "B", Style = "S" };
        var notHad = new Beer { Name = "Not Had Beer", Brewery = "B", Style = "S" };
        context.Beers.AddRange(had, notHad);
        await context.SaveChangesAsync();
        context.BeerConfirmations.Add(new BeerConfirmation { CustomerId = CustomerId, BeerId = had.Id, TavernId = 1, ConfirmedByUserId = "b1" });
        await context.SaveChangesAsync();
        var controller = CreateController(context, userId: CustomerId);

        var result = await controller.GetBeers(hadStatus: "had");

        var response = Assert.IsType<BeerSearchResponse>(result.Value);
        Assert.Equal(new[] { "Had Beer" }, response.Items.Select(b => b.Name));
        Assert.True(response.Items.Single().Confirmed);
    }

    [Fact]
    public async Task GetBeers_WithHadStatusNotHad_ReturnsOnlyUnconfirmedBeers()
    {
        using var context = CreateContext();
        var had = new Beer { Name = "Had Beer", Brewery = "B", Style = "S" };
        var notHad = new Beer { Name = "Not Had Beer", Brewery = "B", Style = "S" };
        context.Beers.AddRange(had, notHad);
        await context.SaveChangesAsync();
        context.BeerConfirmations.Add(new BeerConfirmation { CustomerId = CustomerId, BeerId = had.Id, TavernId = 1, ConfirmedByUserId = "b1" });
        await context.SaveChangesAsync();
        var controller = CreateController(context, userId: CustomerId);

        var result = await controller.GetBeers(hadStatus: "nothad");

        var response = Assert.IsType<BeerSearchResponse>(result.Value);
        Assert.Equal(new[] { "Not Had Beer" }, response.Items.Select(b => b.Name));
        Assert.False(response.Items.Single().Confirmed);
    }

    [Fact]
    public async Task GetBeers_ForAnonymousCaller_NeverMarksConfirmed()
    {
        using var context = CreateContext();
        var beer = new Beer { Name = "Some Beer", Brewery = "B", Style = "S" };
        context.Beers.Add(beer);
        await context.SaveChangesAsync();
        var controller = CreateController(context, userId: null);

        var result = await controller.GetBeers();

        var response = Assert.IsType<BeerSearchResponse>(result.Value);
        Assert.False(response.Items.Single().Confirmed);
    }

    [Fact]
    public async Task GetBeers_Pagination_ReturnsRequestedPageAndTotalCount()
    {
        using var context = CreateContext();
        for (var i = 0; i < 5; i++)
        {
            context.Beers.Add(new Beer { Name = $"Beer {i:D2}", Brewery = "B", Style = "S" });
        }
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.GetBeers(page: 2, pageSize: 2);

        var response = Assert.IsType<BeerSearchResponse>(result.Value);
        Assert.Equal(new[] { "Beer 02", "Beer 03" }, response.Items.Select(b => b.Name));
        Assert.Equal(2, response.Page);
        Assert.Equal(2, response.PageSize);
        Assert.Equal(5, response.TotalCount);
    }

    [Fact]
    public async Task GetBeer_WithKnownId_ReturnsBeer()
    {
        using var context = CreateContext();
        var beer = new Beer { Name = "Hefeweizen", Brewery = "Weihenstephaner", Style = "Wheat" };
        context.Beers.Add(beer);
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.GetBeer(beer.Id);

        Assert.Equal("Hefeweizen", result.Value?.Name);
    }

    [Fact]
    public async Task GetBeer_WithUnknownId_ReturnsNotFound()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetBeer(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetBeer_IncludesBeerNerdStats()
    {
        using var context = CreateContext();
        var beer = new Beer
        {
            Name = "60 Minute IPA",
            Brewery = "Dogfish Head",
            Style = "IPA",
            Abv = 6.0,
            Ibu = 60,
            StyleFamily = "IPA",
            Class = BeerClass.Ale,
        };
        context.Beers.Add(beer);
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.GetBeer(beer.Id);

        Assert.Equal(6.0, result.Value?.Abv);
        Assert.Equal(60, result.Value?.Ibu);
        Assert.Equal("IPA", result.Value?.StyleFamily);
        Assert.Equal(BeerClass.Ale, result.Value?.Class);
    }

    [Fact]
    public async Task GetBeer_WithoutObdbBreweryId_ReturnsNullBreweryInfo()
    {
        using var context = CreateContext();
        var beer = new Beer { Name = "Hefeweizen", Brewery = "Weihenstephaner", Style = "Wheat" };
        context.Beers.Add(beer);
        await context.SaveChangesAsync();
        var lookup = new FakeBreweryLookupService { Result = new BreweryInfo("x", "Should not be called", null, null, null, null) };
        var controller = CreateController(context, breweryLookup: lookup);

        var result = await controller.GetBeer(beer.Id);

        Assert.Null(result.Value?.BreweryInfo);
        Assert.Null(lookup.LastRequestedId);
    }

    [Fact]
    public async Task GetBeer_WithObdbBreweryId_ReturnsBreweryInfoFromLookupService()
    {
        using var context = CreateContext();
        var beer = new Beer { Name = "Hefeweizen", Brewery = "Weihenstephaner", Style = "Wheat", ObdbBreweryId = "obdb-1" };
        context.Beers.Add(beer);
        await context.SaveChangesAsync();
        var breweryInfo = new BreweryInfo("obdb-1", "Weihenstephaner", "large", "Freising", "Bavaria", "https://weihenstephaner.de");
        var lookup = new FakeBreweryLookupService { Result = breweryInfo };
        var controller = CreateController(context, breweryLookup: lookup);

        var result = await controller.GetBeer(beer.Id);

        Assert.Equal("obdb-1", lookup.LastRequestedId);
        Assert.Equal(breweryInfo, result.Value?.BreweryInfo);
    }

    [Fact]
    public async Task GetBeer_WhenBreweryLookupReturnsNull_StillReturnsBeer()
    {
        using var context = CreateContext();
        var beer = new Beer { Name = "Hefeweizen", Brewery = "Weihenstephaner", Style = "Wheat", ObdbBreweryId = "obdb-1" };
        context.Beers.Add(beer);
        await context.SaveChangesAsync();
        var lookup = new FakeBreweryLookupService { Result = null };
        var controller = CreateController(context, breweryLookup: lookup);

        var result = await controller.GetBeer(beer.Id);

        Assert.Equal("Hefeweizen", result.Value?.Name);
        Assert.Null(result.Value?.BreweryInfo);
    }

    [Fact]
    public async Task PostBeer_AddsBeer_AndReturnsCreatedAtAction()
    {
        using var context = CreateContext();
        var controller = CreateController(context, userId: "admin-1");
        var beer = new Beer { Name = "Duvel", Brewery = "Duvel Moortgat", Style = "Belgian Strong Golden Ale" };

        var result = await controller.PostBeer(beer);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(BeersController.GetBeer), created.ActionName);
        Assert.Single(context.Beers);
    }

    [Fact]
    public async Task PostBeer_WritesCreateAudit()
    {
        using var context = CreateContext();
        var controller = CreateController(context, userId: "admin-1");
        var beer = new Beer { Name = "Duvel", Brewery = "Duvel Moortgat", Style = "Belgian Strong Golden Ale" };

        await controller.PostBeer(beer);

        var audit = Assert.Single(context.AdminAudits);
        Assert.Equal("admin-1", audit.AdminUserId);
        Assert.Equal("Beer", audit.EntityType);
        Assert.Equal("Create", audit.Action);
        Assert.Null(audit.BeforeSnapshot);
        Assert.Equal("Duvel (Duvel Moortgat)", audit.AfterSnapshot);
    }

    [Fact]
    public void Beer_DefaultsToAvailable()
    {
        var beer = new Beer { Name = "Duvel", Brewery = "Duvel Moortgat", Style = "Belgian Strong Golden Ale" };

        Assert.Equal(BeerAvailability.Available, beer.Availability);
    }

    [Fact]
    public async Task PostBeer_PersistsExplicitAvailability()
    {
        using var context = CreateContext();
        var controller = CreateController(context, userId: "admin-1");
        var beer = new Beer
        {
            Name = "Winter Bock",
            Brewery = "Some Seasonal Brewery",
            Style = "Bock",
            Availability = BeerAvailability.OutOfStock,
        };

        await controller.PostBeer(beer);

        var saved = await context.Beers.FirstAsync(b => b.Name == "Winter Bock");
        Assert.Equal(BeerAvailability.OutOfStock, saved.Availability);
    }

    [Fact]
    public async Task PutBeer_WithMismatchedId_ReturnsBadRequest()
    {
        using var context = CreateContext();
        var controller = CreateController(context);
        var beer = new Beer { Id = 1, Name = "Fat Tire", Brewery = "New Belgium", Style = "Amber Ale" };

        var result = await controller.PutBeer(2, beer);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task PutBeer_WithMatchingId_UpdatesAndReturnsNoContent()
    {
        using var context = CreateContext();
        var beer = new Beer { Name = "Fat Tire", Brewery = "New Belgium", Style = "Amber Ale" };
        context.Beers.Add(beer);
        await context.SaveChangesAsync();
        context.Entry(beer).State = EntityState.Detached;

        var controller = CreateController(context, userId: "admin-1");
        var updated = new Beer { Id = beer.Id, Name = "Fat Tire", Brewery = "New Belgium", Style = "Amber Ale (updated)" };

        var result = await controller.PutBeer(beer.Id, updated);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("Amber Ale (updated)", (await context.Beers.FindAsync(beer.Id))?.Style);
    }

    [Fact]
    public async Task PutBeer_UnknownId_ReturnsNotFound()
    {
        using var context = CreateContext();
        var controller = CreateController(context, userId: "admin-1");
        var beer = new Beer { Id = 999, Name = "Fat Tire", Brewery = "New Belgium", Style = "Amber Ale" };

        var result = await controller.PutBeer(999, beer);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PutBeer_WithChangedFields_WritesAuditWithOnlyChangedFields()
    {
        using var context = CreateContext();
        var beer = new Beer
        {
            Name = "Fat Tire", Brewery = "New Belgium", Style = "Amber Ale",
            Abv = 5.2, Ibu = 22,
        };
        context.Beers.Add(beer);
        await context.SaveChangesAsync();
        context.Entry(beer).State = EntityState.Detached;

        var controller = CreateController(context, userId: "admin-1");
        var updated = new Beer
        {
            Id = beer.Id, Name = "Fat Tire", Brewery = "New Belgium", Style = "Belgian Pale Ale",
            Abv = 5.5, Ibu = 22,
        };

        var result = await controller.PutBeer(beer.Id, updated);

        Assert.IsType<NoContentResult>(result);
        var audit = Assert.Single(context.AdminAudits);
        Assert.Equal("admin-1", audit.AdminUserId);
        Assert.Equal("Beer", audit.EntityType);
        Assert.Equal(beer.Id.ToString(), audit.EntityId);
        Assert.Equal("Edit", audit.Action);
        Assert.Equal("Style: Amber Ale; Abv: 5.2", audit.BeforeSnapshot);
        Assert.Equal("Style: Belgian Pale Ale; Abv: 5.5", audit.AfterSnapshot);
        Assert.Equal(string.Empty, audit.Reason);
    }

    [Fact]
    public async Task PutBeer_WithNoActualChanges_WritesNoAudit()
    {
        using var context = CreateContext();
        var beer = new Beer { Name = "Fat Tire", Brewery = "New Belgium", Style = "Amber Ale" };
        context.Beers.Add(beer);
        await context.SaveChangesAsync();
        context.Entry(beer).State = EntityState.Detached;

        var controller = CreateController(context, userId: "admin-1");
        var resubmitted = new Beer { Id = beer.Id, Name = "Fat Tire", Brewery = "New Belgium", Style = "Amber Ale" };

        var result = await controller.PutBeer(beer.Id, resubmitted);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(context.AdminAudits);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteBeer_WithoutReason_ReturnsBadRequest_AndDoesNotDelete(string? reason)
    {
        using var context = CreateContext();
        var beer = new Beer { Name = "Pilsner Urquell", Brewery = "Plzeňský Prazdroj", Style = "Czech Pilsner" };
        context.Beers.Add(beer);
        await context.SaveChangesAsync();
        var controller = CreateController(context, userId: "admin-1");

        var result = await controller.DeleteBeer(beer.Id, reason);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Single(context.Beers);
        Assert.Empty(context.AdminAudits);
    }

    [Fact]
    public async Task DeleteBeer_WithReason_RemovesBeer_AndWritesAudit()
    {
        using var context = CreateContext();
        var beer = new Beer { Name = "Pilsner Urquell", Brewery = "Plzeňský Prazdroj", Style = "Czech Pilsner" };
        context.Beers.Add(beer);
        await context.SaveChangesAsync();
        var controller = CreateController(context, userId: "admin-1");

        var result = await controller.DeleteBeer(beer.Id, "discontinued by brewery");

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(context.Beers);
        var audit = Assert.Single(context.AdminAudits);
        Assert.Equal("admin-1", audit.AdminUserId);
        Assert.Equal("Beer", audit.EntityType);
        Assert.Equal("Delete", audit.Action);
        Assert.Equal("Pilsner Urquell (Plzeňský Prazdroj)", audit.BeforeSnapshot);
        Assert.Null(audit.AfterSnapshot);
        Assert.Equal("discontinued by brewery", audit.Reason);
    }

    [Fact]
    public async Task DeleteBeer_WithUnknownId_ReturnsNotFound()
    {
        using var context = CreateContext();
        var controller = CreateController(context, userId: "admin-1");

        var result = await controller.DeleteBeer(999, "cleanup");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteBeer_WithExistingConfirmations_ReturnsConflict_AndDoesNotDelete()
    {
        // BeerConfirmation.BeerId is a restrict-on-delete FK against real Postgres, which
        // the InMemory provider here doesn't enforce — so this guards the explicit
        // up-front check, not provider-enforced referential integrity.
        using var context = CreateContext();
        var beer = new Beer { Name = "Duvel", Brewery = "Duvel Moortgat", Style = "Belgian Strong Golden Ale" };
        context.Beers.Add(beer);
        await context.SaveChangesAsync();
        context.BeerConfirmations.Add(new BeerConfirmation { CustomerId = "cust-1", BeerId = beer.Id, TavernId = 1, ConfirmedByUserId = "b1" });
        await context.SaveChangesAsync();
        var controller = CreateController(context, userId: "admin-1");

        var result = await controller.DeleteBeer(beer.Id, "discontinued by brewery");

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("can't be deleted", conflict.Value!.ToString());
        Assert.Single(context.Beers);
        Assert.Empty(context.AdminAudits);
    }

    [Fact]
    public async Task UpdateAvailability_ChangesValue_WritesAudit()
    {
        using var context = CreateContext();
        var beer = new Beer { Name = "Winter Bock", Brewery = "Test Brewery", Style = "Bock", Availability = BeerAvailability.OnTap };
        context.Beers.Add(beer);
        await context.SaveChangesAsync();
        var controller = CreateController(context, userId: "admin-1");

        var result = await controller.UpdateAvailability(beer.Id, new UpdateAvailabilityRequest(BeerAvailability.OutOfStock));

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(BeerAvailability.OutOfStock, (await context.Beers.FindAsync(beer.Id))!.Availability);
        var audit = Assert.Single(context.AdminAudits);
        Assert.Equal("Beer", audit.EntityType);
        Assert.Equal("AvailabilityChange", audit.Action);
        Assert.Equal("OnTap", audit.BeforeSnapshot);
        Assert.Equal("OutOfStock", audit.AfterSnapshot);
    }

    [Fact]
    public async Task UpdateAvailability_SameValue_IsNoOp_WritesNoAudit()
    {
        using var context = CreateContext();
        var beer = new Beer { Name = "Winter Bock", Brewery = "Test Brewery", Style = "Bock", Availability = BeerAvailability.OnTap };
        context.Beers.Add(beer);
        await context.SaveChangesAsync();
        var controller = CreateController(context, userId: "admin-1");

        var result = await controller.UpdateAvailability(beer.Id, new UpdateAvailabilityRequest(BeerAvailability.OnTap));

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(context.AdminAudits);
    }

    [Fact]
    public async Task UpdateAvailability_UnknownId_ReturnsNotFound()
    {
        using var context = CreateContext();
        var controller = CreateController(context, userId: "admin-1");

        var result = await controller.UpdateAvailability(999, new UpdateAvailabilityRequest(BeerAvailability.Retired));

        Assert.IsType<NotFoundResult>(result);
    }
}
