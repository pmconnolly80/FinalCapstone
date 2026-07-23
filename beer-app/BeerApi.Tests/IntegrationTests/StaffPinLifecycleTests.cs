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

// HTTP-level coverage of #13: role gating on the PIN endpoints and the full lifecycle
// (issue → confirm → reset → deactivate) through the real pipeline.
[Collection("WebApplicationFactory")]
public class StaffPinLifecycleTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public StaffPinLifecycleTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task PinEndpoints_WithoutToken_ReturnUnauthorized()
    {
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _client.PutAsJsonAsync("/api/staff-pins/me", new SetPinRequest("222222"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _client.PutAsJsonAsync("/api/staff-pins/some-user", new SetPinRequest("222222"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _client.DeleteAsync("/api/staff-pins/some-user")).StatusCode);
    }

    [Fact]
    public async Task PinEndpoints_WithCustomerToken_ReturnForbidden()
    {
        var token = await RegisterCustomerAsync("pin.customer@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await _client.PutAsJsonAsync("/api/staff-pins/me", new SetPinRequest("222222"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _client.PutAsJsonAsync("/api/staff-pins/some-user", new SetPinRequest("222222"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _client.DeleteAsync("/api/staff-pins/some-user")).StatusCode);
    }

    [Fact]
    public async Task Bartender_ChangesOwnPin_NewPinConfirms_OldPinRejected()
    {
        var bartenderToken = await LoginAsync("bartender@example.com", "Bartender1!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bartenderToken);
        var change = await _client.PutAsJsonAsync("/api/staff-pins/me", new SetPinRequest("777777"));
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        var customerToken = await RegisterCustomerAsync("own.pin.flow@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        var beers = (await _client.GetFromJsonAsync<BeerSearchResponse>("/api/beers"))!.Items;

        var oldPin = await _client.PostAsJsonAsync("/api/confirmations",
            new ConfirmationRequest(beers![0].Id, SeedData.DevBartenderPin));
        Assert.Equal(HttpStatusCode.Unauthorized, oldPin.StatusCode);

        var newPin = await _client.PostAsJsonAsync("/api/confirmations",
            new ConfirmationRequest(beers[0].Id, "777777"));
        Assert.Equal(HttpStatusCode.Created, newPin.StatusCode);
    }

    [Fact]
    public async Task Admin_IssuesResetsDeactivates_ConfirmFlowTracksEachStep()
    {
        var newBartenderId = await CreateStaffUserAsync("new.bartender@example.com", "Bartender");
        var adminToken = await CreateAdminAndLoginAsync("pin.admin@example.com", "AdminPassw0rd!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Issue — but a PIN colliding with the seeded bartender's active PIN is rejected.
        var collision = await _client.PutAsJsonAsync($"/api/staff-pins/{newBartenderId}",
            new SetPinRequest(SeedData.DevBartenderPin));
        Assert.Equal(HttpStatusCode.Conflict, collision.StatusCode);

        var issue = await _client.PutAsJsonAsync($"/api/staff-pins/{newBartenderId}", new SetPinRequest("222222"));
        Assert.Equal(HttpStatusCode.NoContent, issue.StatusCode);

        var customerToken = await RegisterCustomerAsync("admin.pin.flow@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        var beers = (await _client.GetFromJsonAsync<BeerSearchResponse>("/api/beers"))!.Items;
        var confirmIssued = await _client.PostAsJsonAsync("/api/confirmations",
            new ConfirmationRequest(beers![0].Id, "222222"));
        Assert.Equal(HttpStatusCode.Created, confirmIssued.StatusCode);

        // Reset — the old PIN stops working, the new one confirms.
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var reset = await _client.PutAsJsonAsync($"/api/staff-pins/{newBartenderId}", new SetPinRequest("333333"));
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        var confirmOld = await _client.PostAsJsonAsync("/api/confirmations",
            new ConfirmationRequest(beers[1].Id, "222222"));
        Assert.Equal(HttpStatusCode.Unauthorized, confirmOld.StatusCode);
        var confirmReset = await _client.PostAsJsonAsync("/api/confirmations",
            new ConfirmationRequest(beers[1].Id, "333333"));
        Assert.Equal(HttpStatusCode.Created, confirmReset.StatusCode);

        // Deactivate — the PIN no longer confirms at all.
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var deactivate = await _client.DeleteAsync($"/api/staff-pins/{newBartenderId}");
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        var confirmDeactivated = await _client.PostAsJsonAsync("/api/confirmations",
            new ConfirmationRequest(beers[2].Id, "333333"));
        Assert.Equal(HttpStatusCode.Unauthorized, confirmDeactivated.StatusCode);
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

    private async Task<string> CreateStaffUserAsync(string email, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = email, Email = email };
        await userManager.CreateAsync(user, "StaffPassw0rd!");
        await userManager.AddToRoleAsync(user, role);
        return user.Id;
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
