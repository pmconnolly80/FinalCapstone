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

// #72: customer-facing "look up any beer" search. Role gating and the rate-limit policy
// are HTTP-level behavior covered by BeerLookupTests; this covers the combined-results
// shape and the ExternalSearchLog write.
public class BeerLookupControllerTests
{
    private const string CustomerId = "customer-1";

    private class FakeCatalogBeerService : ICatalogBeerService
    {
        public IReadOnlyList<CatalogBeerResult> Results { get; set; } = Array.Empty<CatalogBeerResult>();

        public Task<IReadOnlyList<CatalogBeerResult>> SearchAsync(string query) => Task.FromResult(Results);
    }

    private class FakeBreweryLookupService : IBreweryLookupService
    {
        public IReadOnlyList<BreweryInfo> Results { get; set; } = Array.Empty<BreweryInfo>();

        public Task<BreweryInfo?> GetBreweryAsync(string breweryId) => Task.FromResult<BreweryInfo?>(null);

        public Task<IReadOnlyList<BreweryInfo>> SearchBreweriesAsync(string query) => Task.FromResult(Results);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static BeerLookupController CreateController(
        ApplicationDbContext context, ICatalogBeerService catalogBeer, IBreweryLookupService breweryLookup,
        string? userId = CustomerId)
    {
        var claims = userId == null
            ? new ClaimsIdentity()
            : new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test");

        return new BeerLookupController(context, catalogBeer, breweryLookup)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(claims) },
            },
        };
    }

    [Fact]
    public async Task Search_ReturnsCombinedBeerAndBreweryResults()
    {
        using var context = CreateContext();
        var beerResult = new CatalogBeerResult("cb-1", "Duvel", "Belgian Tripel", "Belgian Ale", "Ale", "Light-bodied.", 8.5, null, true, "Duvel Moortgat");
        var breweryResult = new BreweryInfo("obdb-1", "Duvel Moortgat", "regional", "Breendonk", "Belgium", "https://duvel.com");
        var catalogBeer = new FakeCatalogBeerService { Results = new[] { beerResult } };
        var breweryLookup = new FakeBreweryLookupService { Results = new[] { breweryResult } };
        var controller = CreateController(context, catalogBeer, breweryLookup);

        var result = await controller.Search("duvel");

        var response = Assert.IsType<BeerLookupResponse>(result.Value);
        Assert.Equal(new[] { beerResult }, response.Beers);
        Assert.Equal(new[] { breweryResult }, response.Breweries);
    }

    [Fact]
    public async Task Search_BlankQuery_ReturnsEmptyResults_AndLogsNothing()
    {
        using var context = CreateContext();
        var controller = CreateController(context, new FakeCatalogBeerService(), new FakeBreweryLookupService());

        var result = await controller.Search("   ");

        var response = Assert.IsType<BeerLookupResponse>(result.Value);
        Assert.Empty(response.Beers);
        Assert.Empty(response.Breweries);
        Assert.Empty(context.ExternalSearchLogs);
    }

    [Fact]
    public async Task Search_LogsQuery_WithCustomerIdAndMatchedTavernCatalogTrue()
    {
        using var context = CreateContext();
        context.Beers.Add(new Beer { Name = "Duvel", Brewery = "Duvel Moortgat", Style = "Belgian Strong Golden Ale" });
        await context.SaveChangesAsync();
        var controller = CreateController(context, new FakeCatalogBeerService(), new FakeBreweryLookupService());

        await controller.Search("duvel");

        var log = Assert.Single(context.ExternalSearchLogs);
        Assert.Equal(CustomerId, log.CustomerId);
        Assert.Equal("duvel", log.Query);
        Assert.True(log.MatchedTavernCatalog);
    }

    [Fact]
    public async Task Search_LogsMatchedTavernCatalogFalse_WhenNoLocalMatch()
    {
        using var context = CreateContext();
        context.Beers.Add(new Beer { Name = "Duvel", Brewery = "Duvel Moortgat", Style = "Belgian Strong Golden Ale" });
        await context.SaveChangesAsync();
        var controller = CreateController(context, new FakeCatalogBeerService(), new FakeBreweryLookupService());

        await controller.Search("some beer nobody stocks");

        var log = Assert.Single(context.ExternalSearchLogs);
        Assert.False(log.MatchedTavernCatalog);
    }

    [Fact]
    public async Task Search_WithoutCustomerId_ReturnsUnauthorized()
    {
        using var context = CreateContext();
        var controller = CreateController(context, new FakeCatalogBeerService(), new FakeBreweryLookupService(), userId: null);

        var result = await controller.Search("duvel");

        Assert.IsType<UnauthorizedResult>(result.Result);
        Assert.Empty(context.ExternalSearchLogs);
    }
}
