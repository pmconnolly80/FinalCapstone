using System.Net;
using System.Text;
using BeerApi.Services;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace BeerApi.Tests.Services;

public class OpenBreweryDbServiceTests
{
    private class FakeHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } =
            _ => new HttpResponseMessage(HttpStatusCode.NotFound);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(Respond(request));
        }
    }

    private static (OpenBreweryDbService Service, FakeHandler Handler) CreateService()
    {
        var handler = new FakeHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openbrewerydb.org/v1/") };
        var cache = new MemoryCache(new MemoryCacheOptions());
        return (new OpenBreweryDbService(httpClient, cache), handler);
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    [Fact]
    public async Task GetBreweryAsync_ReturnsMappedFields_OnSuccess()
    {
        var (service, handler) = CreateService();
        handler.Respond = _ => JsonResponse(
            "{\"id\":\"abc-123\",\"name\":\"Sierra Nevada\",\"brewery_type\":\"regional\"," +
            "\"city\":\"Chico\",\"state_province\":\"California\",\"website_url\":\"https://sierranevada.com\"}");

        var result = await service.GetBreweryAsync("abc-123");

        Assert.NotNull(result);
        Assert.Equal("abc-123", result!.Id);
        Assert.Equal("Sierra Nevada", result.Name);
        Assert.Equal("regional", result.BreweryType);
        Assert.Equal("Chico", result.City);
        Assert.Equal("California", result.State);
        Assert.Equal("https://sierranevada.com", result.WebsiteUrl);
    }

    [Fact]
    public async Task GetBreweryAsync_CachesSuccessfulLookups_DoesNotRefetch()
    {
        var (service, handler) = CreateService();
        handler.Respond = _ => JsonResponse("{\"id\":\"abc-123\",\"name\":\"Sierra Nevada\"}");

        await service.GetBreweryAsync("abc-123");
        await service.GetBreweryAsync("abc-123");

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GetBreweryAsync_ReturnsNull_OnNotFound()
    {
        var (service, handler) = CreateService();
        handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.NotFound);

        var result = await service.GetBreweryAsync("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBreweryAsync_ReturnsNull_WhenObdbIsUnreachable()
    {
        var (service, handler) = CreateService();
        handler.Respond = _ => throw new HttpRequestException("network down");

        var result = await service.GetBreweryAsync("abc-123");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBreweryAsync_DoesNotCacheFailures_RetriesNextCall()
    {
        var (service, handler) = CreateService();
        handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.NotFound);

        await service.GetBreweryAsync("abc-123");
        await service.GetBreweryAsync("abc-123");

        Assert.Equal(2, handler.CallCount);
    }
}
