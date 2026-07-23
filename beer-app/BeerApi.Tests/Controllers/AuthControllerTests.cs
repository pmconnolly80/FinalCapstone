using System.Net;
using System.Net.Http.Json;
using BeerApi.Controllers;
using BeerApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BeerApi.Tests.Controllers;

[Collection("WebApplicationFactory")]
public class AuthControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public AuthControllerTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithNewEmail_CreatesCustomer_AndReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("new.customer@example.com", "Passw0rd!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body?.Token));
        Assert.Equal("new.customer@example.com", body?.Email);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict()
    {
        var request = new RegisterRequest("duplicate@example.com", "Passw0rd!");
        await _client.PostAsJsonAsync("/api/auth/register", request);

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithMissingPassword_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("no.password@example.com", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsToken()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("login.success@example.com", "Passw0rd!"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("login.success@example.com", "Passw0rd!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body?.Token));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("login.wrongpass@example.com", "Passw0rd!"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("login.wrongpass@example.com", "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("does.not.exist@example.com", "Passw0rd!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // #17: the paper-sheet crowd types passwords like "beer1234", not "Passw0rd!".
    // Policy is length-only (min 8) so the one rule we enforce is the one we can explain.
    [Fact]
    public async Task Register_WithCasualPasswordMeetingLength_Succeeds()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("casual.password@example.com", "beer1234"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body?.Token));
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest_WithExplanation()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("short.password@example.com", "beer123"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Contains("at least 8 characters", body?.Message);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict_WithExplanation()
    {
        var request = new RegisterRequest("duplicate.message@example.com", "Passw0rd!");
        await _client.PostAsJsonAsync("/api/auth/register", request);

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("A user with that email already exists.", body?.Message);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsGenericMessage()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("login.message@example.com", "Passw0rd!"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("login.message@example.com", "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("Invalid credentials.", body?.Message);
    }

    [Fact]
    public async Task Register_WithMarketingConsentTrue_PersistsConsent()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("consent.yes@example.com", "Passw0rd!", MarketingConsent: true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("consent.yes@example.com");

        Assert.True(user?.MarketingConsent);
    }

    [Fact]
    public async Task Register_WithoutMarketingConsent_DefaultsToFalse()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("consent.default@example.com", "Passw0rd!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("consent.default@example.com");

        Assert.False(user?.MarketingConsent);
    }

    public void Dispose() => _factory.Dispose();

    private sealed record ErrorResponse(string Message);
}
