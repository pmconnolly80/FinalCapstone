using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BeerApi.Controllers;
using BeerApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BeerApi.Tests.IntegrationTests;

// HTTP-level gating for the owner's mug-earner list (#14).
[Collection("WebApplicationFactory")]
public class MugAwardsTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public MugAwardsTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetMugAwards_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/mug-awards");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMugAwards_WithCustomerToken_ReturnsForbidden()
    {
        var register = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("award.customer@example.com", "Passw0rd!"));
        var body = await register.Content.ReadFromJsonAsync<AuthResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);

        var response = await _client.GetAsync("/api/mug-awards");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMugAwards_WithAdminToken_ReturnsList()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = new ApplicationUser { UserName = "award.admin@example.com", Email = "award.admin@example.com" };
            await userManager.CreateAsync(admin, "AdminPassw0rd!");
            await userManager.AddToRoleAsync(admin, "Admin");
        }
        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("award.admin@example.com", "AdminPassw0rd!"));
        var body = await login.Content.ReadFromJsonAsync<AuthResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);

        var response = await _client.GetAsync("/api/mug-awards");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var awards = await response.Content.ReadFromJsonAsync<List<MugAwardResponse>>();
        Assert.NotNull(awards);
    }

    public void Dispose() => _factory.Dispose();
}
