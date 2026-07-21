using System.Net;
using System.Text;
using BeerApi.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BeerApi.Tests.Services;

public class CatalogBeerServiceTests
{
    private class FakeHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }
        public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } =
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"data\":[]}") };

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            return Task.FromResult(Respond(request));
        }
    }

    private static (CatalogBeerService Service, FakeHandler Handler) CreateService(string? apiKey = "test-api-key")
    {
        var handler = new FakeHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.catalog.beer/") };
        var cache = new MemoryCache(new MemoryCacheOptions());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(apiKey == null
                ? Array.Empty<KeyValuePair<string, string?>>()
                : new[] { new KeyValuePair<string, string?>("CatalogBeer:ApiKey", apiKey) })
            .Build();
        return (new CatalogBeerService(httpClient, cache, config), handler);
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    [Fact]
    public async Task SearchAsync_ReturnsMappedResults()
    {
        var (service, handler) = CreateService();
        handler.Respond = _ => JsonResponse(
            "{\"data\":[{\"id\":\"abc\",\"name\":\"Duvel\",\"style\":\"Belgian-Style Tripel\",\"parent\":\"Belgian Ale\"," +
            "\"class\":\"ale\",\"description\":\"Deceptively light.\",\"abv\":8.5,\"ibu\":null,\"cb_verified\":true," +
            "\"brewer\":{\"name\":\"Duvel Moortgat\"}}]}");

        var results = await service.SearchAsync("duvel");

        var result = Assert.Single(results);
        Assert.Equal("abc", result.Id);
        Assert.Equal("Duvel", result.Name);
        Assert.Equal("Belgian-Style Tripel", result.Style);
        Assert.Equal("Belgian Ale", result.StyleFamily);
        Assert.Equal("Ale", result.Class);
        Assert.Equal("Deceptively light.", result.Description);
        Assert.Equal(8.5, result.Abv);
        Assert.Null(result.Ibu);
        Assert.True(result.CbVerified);
        Assert.Equal("Duvel Moortgat", result.BrewerName);
    }

    [Fact]
    public async Task SearchAsync_SortsVerifiedResultsFirst()
    {
        var (service, handler) = CreateService();
        handler.Respond = _ => JsonResponse(
            "{\"data\":[" +
            "{\"id\":\"unverified\",\"name\":\"Beer A\",\"cb_verified\":false}," +
            "{\"id\":\"verified\",\"name\":\"Beer B\",\"cb_verified\":true}" +
            "]}");

        var results = await service.SearchAsync("beer");

        Assert.Equal("verified", results[0].Id);
        Assert.Equal("unverified", results[1].Id);
    }

    [Fact]
    public async Task SearchAsync_SendsBasicAuthHeaderWithConfiguredKey()
    {
        var (service, handler) = CreateService(apiKey: "my-key");

        await service.SearchAsync("duvel");

        var expected = Convert.ToBase64String(Encoding.UTF8.GetBytes("my-key:"));
        Assert.Equal("Basic", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal(expected, handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SearchAsync_CachesByQuery_DoesNotRefetch()
    {
        var (service, handler) = CreateService();
        handler.Respond = _ => JsonResponse("{\"data\":[{\"id\":\"abc\",\"name\":\"Duvel\"}]}");

        await service.SearchAsync("duvel");
        await service.SearchAsync("duvel");

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_WhenUnreachable()
    {
        var (service, handler) = CreateService();
        handler.Respond = _ => throw new HttpRequestException("network down");

        var results = await service.SearchAsync("duvel");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_WithoutCallingApi_WhenNoKeyConfigured()
    {
        var (service, handler) = CreateService(apiKey: null);

        var results = await service.SearchAsync("duvel");

        Assert.Empty(results);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_ForBlankQuery()
    {
        var (service, handler) = CreateService();

        var results = await service.SearchAsync("   ");

        Assert.Empty(results);
        Assert.Equal(0, handler.CallCount);
    }
}
