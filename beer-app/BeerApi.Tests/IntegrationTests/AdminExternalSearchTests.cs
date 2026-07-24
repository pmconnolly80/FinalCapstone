using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BeerApi.Controllers;
using BeerApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BeerApi.Tests.IntegrationTests;

// #83: role gating on GET /api/admin/external-search-demand. The aggregation logic
// itself is unit-tested directly (AdminExternalSearchControllerTests) with a fixed
// reference time.
[Collection("WebApplicationFactory")]
public class AdminExternalSearchTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public AdminExternalSearchTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetDemand_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/admin/external-search-demand");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDemand_WithCustomerToken_ReturnsForbidden()
    {
        var token = await RegisterCustomerAsync("demand.customer@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/external-search-demand");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDemand_WithAdminToken_ReturnsOk()
    {
        var token = await CreateAdminAndLoginAsync("demand.admin@example.com", "AdminPassw0rd!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/external-search-demand");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
