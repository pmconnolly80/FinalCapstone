using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BeerApi.Controllers;
using BeerApi.Data;
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

    [Fact]
    public async Task PutBeer_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PutAsJsonAsync("/api/beers/1", new Beer { Id = 1, Name = "X", Brewery = "Y", Style = "Z" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutBeer_WithCustomerToken_ReturnsForbidden()
    {
        var token = await RegisterCustomerAsync("put.customer@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsJsonAsync("/api/beers/1", new Beer { Id = 1, Name = "X", Brewery = "Y", Style = "Z" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBeer_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.DeleteAsync("/api/beers/1?reason=cleanup");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBeer_WithCustomerToken_ReturnsForbidden()
    {
        var token = await RegisterCustomerAsync("delete.customer@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.DeleteAsync("/api/beers/1?reason=cleanup");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAvailability_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PatchAsJsonAsync("/api/beers/1/availability", new UpdateAvailabilityRequest(BeerAvailability.Retired));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAvailability_WithCustomerToken_ReturnsForbidden()
    {
        var token = await RegisterCustomerAsync("patch.customer@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/beers/1/availability", new UpdateAvailabilityRequest(BeerAvailability.Retired));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminFlow_EditsAvailabilityFlipsAndDeletes_AllAudited()
    {
        var adminToken = await CreateAdminAndLoginAsync("beer-audit-admin@example.com", "AdminPassw0rd!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var created = await _client.PostAsJsonAsync("/api/beers",
            new Beer { Name = "Audit Test Beer", Brewery = "Audit Brewery", Style = "Porter", Availability = BeerAvailability.OnTap });
        var beer = await created.Content.ReadFromJsonAsync<Beer>();

        var edited = await _client.PutAsJsonAsync($"/api/beers/{beer!.Id}",
            new Beer { Id = beer.Id, Name = "Audit Test Beer", Brewery = "Audit Brewery", Style = "Stout", Availability = BeerAvailability.OnTap });
        Assert.Equal(HttpStatusCode.NoContent, edited.StatusCode);

        var patched = await _client.PatchAsJsonAsync($"/api/beers/{beer.Id}/availability",
            new UpdateAvailabilityRequest(BeerAvailability.OutOfStock));
        Assert.Equal(HttpStatusCode.NoContent, patched.StatusCode);

        var noReason = await _client.DeleteAsync($"/api/beers/{beer.Id}?reason=");
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);

        var deleted = await _client.DeleteAsync($"/api/beers/{beer.Id}?reason=discontinued");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var audits = context.AdminAudits.Where(a => a.EntityId == beer.Id.ToString()).ToList();

        var editAudit = Assert.Single(audits, a => a.Action == "Edit");
        Assert.Contains("Style: Porter", editAudit.BeforeSnapshot);
        Assert.Contains("Style: Stout", editAudit.AfterSnapshot);

        var availabilityAudit = Assert.Single(audits, a => a.Action == "AvailabilityChange");
        Assert.Equal("OnTap", availabilityAudit.BeforeSnapshot);
        Assert.Equal("OutOfStock", availabilityAudit.AfterSnapshot);

        var deleteAudit = Assert.Single(audits, a => a.Action == "Delete");
        Assert.Equal("discontinued", deleteAudit.Reason);

        Assert.Null(await context.Beers.FindAsync(beer.Id));
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
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = email, Email = email };
        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, "Admin");

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }

    public void Dispose() => _factory.Dispose();
}
