using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BeerApi.Controllers;
using BeerApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BeerApi.Tests.IntegrationTests;

// HTTP-level coverage of #58: role gating on GET /api/admin/anomalies. The detection
// logic itself is unit-tested directly (AdminAnomaliesControllerTests) with a fixed
// reference time; this just confirms the endpoint is wired up and returns a sane
// response for an admin.
[Collection("WebApplicationFactory")]
public class AdminAnomaliesTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public AdminAnomaliesTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAnomalies_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/admin/anomalies");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAnomalies_WithCustomerToken_ReturnsForbidden()
    {
        var token = await RegisterCustomerAsync("anomaly.customer@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/anomalies");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAnomalies_WithAdminToken_ReturnsOk()
    {
        var adminToken = await CreateAdminAndLoginAsync("anomaly.admin@example.com", "AdminPassw0rd!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.GetAsync("/api/admin/anomalies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<AnomalyResponse>>();
        Assert.NotNull(body);
    }

    // #81: a customer's report is invisible via the customer-facing API surface (it
    // never changes availability) but shows up to an admin through this same anomalies
    // endpoint, reusing #58's existing panel rather than a new one.
    [Fact]
    public async Task ReportUnavailable_ThenShowsUpToAdminAsAnAnomaly()
    {
        var customerToken = await RegisterCustomerAsync("report.customer@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        var beers = (await _client.GetFromJsonAsync<BeerApi.Controllers.BeerSearchResponse>("/api/beers"))!.Items;
        var beer = beers![0];

        var reportResponse = await _client.PostAsync($"/api/beers/{beer.Id}/unavailability-reports", null);
        Assert.Equal(HttpStatusCode.NoContent, reportResponse.StatusCode);

        var adminToken = await CreateAdminAndLoginAsync("report.admin@example.com", "AdminPassw0rd!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var anomalies = await _client.GetFromJsonAsync<List<AnomalyResponse>>("/api/admin/anomalies");
        var anomaly = Assert.Single(anomalies!, a => a.Type == "UnavailabilityReport");
        Assert.Contains(beer.Name, anomaly.Summary);
        Assert.Equal($"/beers/{beer.Id}", anomaly.DeepLink);
    }

    [Fact]
    public async Task ReportUnavailable_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/beers/1/unavailability-reports", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReportUnavailable_UnknownBeer_ReturnsNotFound()
    {
        var customerToken = await RegisterCustomerAsync("report.unknown.customer@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        var response = await _client.PostAsync("/api/beers/999999/unavailability-reports", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

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
