using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BeerApi.Controllers;
using Xunit;

namespace BeerApi.Tests.IntegrationTests;

// #72: any signed-in role (not just Admin) can reach this, but it must never be reachable
// anonymously, and repeated calls must eventually 429 given Catalog.beer's cost-sensitive
// free-tier budget. The search-combining/logging logic itself is unit-tested directly
// (BeerLookupControllerTests).
[Collection("WebApplicationFactory")]
public class BeerLookupTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public BeerLookupTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Search_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/beer-lookup/search?query=duvel");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithCustomerToken_ReturnsOk()
    {
        var token = await RegisterCustomerAsync("lookup.customer@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/beer-lookup/search?query=duvel");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Search_ExceedingRateLimit_ReturnsTooManyRequests()
    {
        var token = await RegisterCustomerAsync("lookup.ratelimited@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage? last = null;
        for (var i = 0; i < 21; i++)
        {
            last = await _client.GetAsync("/api/beer-lookup/search?query=duvel");
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }

    private async Task<string> RegisterCustomerAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Passw0rd!"));
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }

    public void Dispose() => _factory.Dispose();
}
