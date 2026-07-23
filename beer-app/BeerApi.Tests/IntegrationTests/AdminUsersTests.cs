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

// HTTP-level coverage of #53: role gating on the admin user-role endpoint, plus the
// role-reassignment flow end to end.
[Collection("WebApplicationFactory")]
public class AdminUsersTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public AdminUsersTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task RoleEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PutAsJsonAsync("/api/admin/users/some-id/role", new AssignRoleRequest("Bartender", "x"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RoleEndpoint_WithCustomerOrBartenderToken_ReturnsForbidden()
    {
        var customerToken = await RegisterCustomerAsync("role.customer@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        var asCustomer = await _client.PutAsJsonAsync("/api/admin/users/some-id/role", new AssignRoleRequest("Bartender", "x"));
        Assert.Equal(HttpStatusCode.Forbidden, asCustomer.StatusCode);

        var bartenderToken = await LoginAsync("bartender@example.com", "Bartender1!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bartenderToken);
        var asBartender = await _client.PutAsJsonAsync("/api/admin/users/some-id/role", new AssignRoleRequest("Bartender", "x"));
        Assert.Equal(HttpStatusCode.Forbidden, asBartender.StatusCode);
    }

    [Fact]
    public async Task RoleFlow_ReassignsRole_AndRecordsAudit()
    {
        var customerToken = await RegisterCustomerAsync("role.target@example.com");
        var targetUserId = await GetUserIdAsync("role.target@example.com");

        var adminToken = await CreateAdminAndLoginAsync("role.admin@example.com", "AdminPassw0rd!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Reason is mandatory — no silent role changes.
        var noReason = await _client.PutAsJsonAsync($"/api/admin/users/{targetUserId}/role",
            new AssignRoleRequest("Bartender", ""));
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);

        var invalidRole = await _client.PutAsJsonAsync($"/api/admin/users/{targetUserId}/role",
            new AssignRoleRequest("SuperAdmin", "promote"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidRole.StatusCode);

        var promoted = await _client.PutAsJsonAsync($"/api/admin/users/{targetUserId}/role",
            new AssignRoleRequest("Bartender", "promoted to staff"));
        Assert.Equal(HttpStatusCode.NoContent, promoted.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(targetUserId);
        var roles = await userManager.GetRolesAsync(user!);
        Assert.Equal(new[] { "Bartender" }, roles);

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var audit = Assert.Single(context.AdminAudits, a => a.EntityId == targetUserId);
        Assert.Equal("RoleChange", audit.Action);
        Assert.Equal("Customer", audit.BeforeSnapshot);
        Assert.Equal("Bartender", audit.AfterSnapshot);
        Assert.Equal("promoted to staff", audit.Reason);
    }

    [Fact]
    public async Task RoleEndpoint_UnknownUserId_ReturnsNotFound()
    {
        var adminToken = await CreateAdminAndLoginAsync("role.admin2@example.com", "AdminPassw0rd!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.PutAsJsonAsync("/api/admin/users/nonexistent-id/role",
            new AssignRoleRequest("Bartender", "promote"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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

    private async Task<string> GetUserIdAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        return user!.Id;
    }

    public void Dispose() => _factory.Dispose();
}
