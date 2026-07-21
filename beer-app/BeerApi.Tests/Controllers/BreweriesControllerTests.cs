using BeerApi.Controllers;
using BeerApi.Services;
using Xunit;

namespace BeerApi.Tests.Controllers;

public class BreweriesControllerTests
{
    private class FakeBreweryLookupService : IBreweryLookupService
    {
        public IReadOnlyList<BreweryInfo> Results { get; set; } = Array.Empty<BreweryInfo>();
        public string? LastQuery { get; private set; }

        public Task<BreweryInfo?> GetBreweryAsync(string breweryId) => Task.FromResult<BreweryInfo?>(null);

        public Task<IReadOnlyList<BreweryInfo>> SearchBreweriesAsync(string query)
        {
            LastQuery = query;
            return Task.FromResult(Results);
        }
    }

    [Fact]
    public async Task Search_PassesQueryThrough_ReturnsServiceResults()
    {
        var breweryInfo = new BreweryInfo("obdb-1", "Sierra Nevada Brewing Co", "regional", "Chico", "California", "https://sierranevada.com");
        var lookup = new FakeBreweryLookupService { Results = new[] { breweryInfo } };
        var controller = new BreweriesController(lookup);

        var result = await controller.Search("sierra");

        Assert.Equal("sierra", lookup.LastQuery);
        Assert.Equal(new[] { breweryInfo }, result);
    }

    [Fact]
    public async Task Search_WithNoResults_ReturnsEmptyList()
    {
        var lookup = new FakeBreweryLookupService();
        var controller = new BreweriesController(lookup);

        var result = await controller.Search("nonexistent-brewery-xyz");

        Assert.Empty(result);
    }
}
