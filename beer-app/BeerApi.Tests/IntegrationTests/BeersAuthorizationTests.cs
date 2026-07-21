using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BeerApi.Controllers;
using BeerApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BeerApi.Tests.IntegrationTests;

[Collection("WebApplicationFactory")]
public class BeersAuthorizationTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public BeersAuthorizationTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetBeers_AllowsAnonymousAccess()
    {
        var response = await _client.GetAsync("/api/beers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostBeer_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/beers", NewBeer());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostBeer_WithCustomerToken_ReturnsForbidden()
    {
        var token = await RegisterCustomerAsync("customer@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/beers", NewBeer());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostBeer_WithAdminToken_ReturnsCreated()
    {
        var token = await CreateAdminAndLoginAsync("admin@example.com", "AdminPassw0rd!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/beers", NewBeer());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetBeers_SerializesAvailabilityAsString()
    {
        var response = await _client.GetAsync("/api/beers");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"availability\":\"Available\"", body);
    }

    [Fact]
    public async Task GetBeers_SearchFiltersAcrossRealHttpRoundTrip()
    {
        var adminToken = await CreateAdminAndLoginAsync("search-admin@example.com", "AdminPassw0rd!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _client.PostAsJsonAsync("/api/beers", new Beer { Name = "Zzyzx Sour", Brewery = "Search Test Brewery", Style = "Sour" });
        _client.DefaultRequestHeaders.Authorization = null;

        var result = await _client.GetFromJsonAsync<BeerSearchResponse>("/api/beers?search=zzyzx");

        Assert.NotNull(result);
        Assert.Equal(1, result!.TotalCount);
        Assert.Equal("Zzyzx Sour", result.Items.Single().Name);
        Assert.False(result.Items.Single().Confirmed);
    }

    [Fact]
    public async Task GetBeers_WithHadStatusAndNoToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/beers?hadStatus=had");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static Beer NewBeer() => new() { Name = "Test Beer", Brewery = "Test Brewery", Style = "Test Style" };

    private async Task<string> RegisterCustomerAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Passw0rd!"));
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }

    private async Task<string> CreateAdminAndLoginAsync(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = new IdentityUser { UserName = email, Email = email };
        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, "Admin");

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }

    public void Dispose() => _factory.Dispose();
}
