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

// HTTP-level coverage of #15: role gating on the admin confirmation endpoints and the
// void flow end to end — confirm, void with reason, progress drops, beer confirmable again.
[Collection("WebApplicationFactory")]
public class AdminConfirmationsTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public AdminConfirmationsTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task AdminEndpoints_WithoutToken_ReturnUnauthorized()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/admin/confirmations")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/admin/confirmations/audits")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _client.PostAsJsonAsync("/api/admin/confirmations/1/void", new VoidConfirmationRequest("x"))).StatusCode);
    }

    [Fact]
    public async Task AdminEndpoints_WithCustomerOrBartenderToken_ReturnForbidden()
    {
        var customerToken = await RegisterCustomerAsync("audit.customer@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/admin/confirmations")).StatusCode);

        var bartenderToken = await LoginAsync("bartender@example.com", "Bartender1!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bartenderToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/admin/confirmations")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _client.PostAsJsonAsync("/api/admin/confirmations/1/void", new VoidConfirmationRequest("x"))).StatusCode);
    }

    [Fact]
    public async Task VoidFlow_ProgressDropsImmediately_AndBeerIsConfirmableAgain()
    {
        var customerToken = await RegisterCustomerAsync("void.flow@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        var beers = (await _client.GetFromJsonAsync<BeerSearchResponse>("/api/beers"))!.Items;
        var confirm = await _client.PostAsJsonAsync("/api/confirmations",
            new ConfirmationRequest(beers![0].Id, SeedData.DevBartenderPin));
        Assert.Equal(HttpStatusCode.Created, confirm.StatusCode);

        var adminToken = await CreateAdminAndLoginAsync("audit.admin@example.com", "AdminPassw0rd!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var list = await _client.GetFromJsonAsync<List<AdminConfirmationResponse>>("/api/admin/confirmations");
        var row = Assert.Single(list!, r => r.CustomerEmail == "void.flow@example.com");

        // Reason is mandatory — no silent deletes.
        var noReason = await _client.PostAsJsonAsync($"/api/admin/confirmations/{row.Id}/void",
            new VoidConfirmationRequest(""));
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);

        var voided = await _client.PostAsJsonAsync($"/api/admin/confirmations/{row.Id}/void",
            new VoidConfirmationRequest("wrong customer confirmed"));
        Assert.Equal(HttpStatusCode.NoContent, voided.StatusCode);

        var audits = await _client.GetFromJsonAsync<List<ConfirmationAuditResponse>>("/api/admin/confirmations/audits");
        var audit = Assert.Single(audits!, a => a.CustomerEmail == "void.flow@example.com");
        Assert.Equal("wrong customer confirmed", audit.Reason);
        Assert.Equal("audit.admin@example.com", audit.AdminEmail);

        // The customer's progress reflects the correction immediately...
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        var progress = await _client.GetFromJsonAsync<ProgressResponse>("/api/me/progress");
        Assert.Equal(0, progress!.ConfirmedCount);

        // ...and the voided beer can be confirmed again.
        var reconfirm = await _client.PostAsJsonAsync("/api/confirmations",
            new ConfirmationRequest(beers[0].Id, SeedData.DevBartenderPin));
        Assert.Equal(HttpStatusCode.Created, reconfirm.StatusCode);
    }

    private async Task<string> RegisterCustomerAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Passw0rd!"));
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
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
        return await LoginAsync(email, password);
    }

    public void Dispose() => _factory.Dispose();
}
