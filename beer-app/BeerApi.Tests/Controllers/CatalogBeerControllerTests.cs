using BeerApi.Controllers;
using BeerApi.Services;
using Xunit;

namespace BeerApi.Tests.Controllers;

public class CatalogBeerControllerTests
{
    private class FakeCatalogBeerService : ICatalogBeerService
    {
        public IReadOnlyList<CatalogBeerResult> Results { get; set; } = Array.Empty<CatalogBeerResult>();
        public string? LastQuery { get; private set; }

        public Task<IReadOnlyList<CatalogBeerResult>> SearchAsync(string query)
        {
            LastQuery = query;
            return Task.FromResult(Results);
        }
    }

    [Fact]
    public async Task Search_PassesQueryThrough_ReturnsServiceResults()
    {
        var result = new CatalogBeerResult("cb-1", "Duvel", "Belgian-Style Tripel", "Belgian Ale", "Ale", "Light-bodied.", 8.5, null, true, "Duvel Moortgat");
        var service = new FakeCatalogBeerService { Results = new[] { result } };
        var controller = new CatalogBeerController(service);

        var response = await controller.Search("duvel");

        Assert.Equal("duvel", service.LastQuery);
        Assert.Equal(new[] { result }, response);
    }

    [Fact]
    public async Task Search_WithNoResults_ReturnsEmptyList()
    {
        var service = new FakeCatalogBeerService();
        var controller = new CatalogBeerController(service);

        var response = await controller.Search("nonexistent-beer-xyz");

        Assert.Empty(response);
    }
}
