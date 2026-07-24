using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BeerApi.Controllers;
using BeerApi.Data;
using BeerApi.Models;
using BeerApi.Tests.TestDoubles;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BeerApi.Tests.IntegrationTests;

// HTTP-level coverage of #53 (role gating on the admin user-role endpoint, plus the
// role-reassignment flow end to end) and #54 (users list, deactivate/reactivate — the
// deactivated user's login is actually blocked, and their PIN goes with it).
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

    [Fact]
    public async Task AccountActionEndpoints_WithoutToken_ReturnUnauthorized()
    {
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _client.GetAsync("/api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _client.PostAsJsonAsync("/api/admin/users/some-id/deactivate", new AccountActionRequest("x"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _client.PostAsJsonAsync("/api/admin/users/some-id/reactivate", new AccountActionRequest("x"))).StatusCode);
    }

    [Fact]
    public async Task AccountActionEndpoints_WithCustomerOrBartenderToken_ReturnForbidden()
    {
        var customerToken = await RegisterCustomerAsync("account.customer@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _client.PostAsJsonAsync("/api/admin/users/some-id/deactivate", new AccountActionRequest("x"))).StatusCode);

        var bartenderToken = await LoginAsync("bartender@example.com", "Bartender1!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bartenderToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _client.PostAsJsonAsync("/api/admin/users/some-id/reactivate", new AccountActionRequest("x"))).StatusCode);
    }

    [Fact]
    public async Task DeactivateEndpoint_UnknownUserId_ReturnsNotFound()
    {
        var adminToken = await CreateAdminAndLoginAsync("account.admin1@example.com", "AdminPassw0rd!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.PostAsJsonAsync("/api/admin/users/nonexistent-id/deactivate",
            new AccountActionRequest("policy violation"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_ListsRoleActiveStatusAndPinPresence()
    {
        var customerToken = await RegisterCustomerAsync("account.list@example.com");
        var targetUserId = await GetUserIdAsync("account.list@example.com");

        var adminToken = await CreateAdminAndLoginAsync("account.admin2@example.com", "AdminPassw0rd!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        await _client.PutAsJsonAsync($"/api/admin/users/{targetUserId}/role", new AssignRoleRequest("Bartender", "staffing up"));
        var pinResult = await _client.PutAsJsonAsync($"/api/staff-pins/{targetUserId}", new SetPinRequest("135790"));
        Assert.Equal(HttpStatusCode.NoContent, pinResult.StatusCode);

        var users = await _client.GetFromJsonAsync<List<AdminUserResponse>>("/api/admin/users");
        var row = Assert.Single(users!, u => u.Id == targetUserId);
        Assert.Equal("account.list@example.com", row.Email);
        Assert.Equal("Bartender", row.Role);
        Assert.True(row.IsActive);
        Assert.True(row.HasActivePin);
    }

    [Fact]
    public async Task AccountFlow_DeactivateBlocksLoginAndDropsPin_ReactivateRestoresLoginNotPin()
    {
        var customerToken = await RegisterCustomerAsync("account.flow@example.com");
        var targetUserId = await GetUserIdAsync("account.flow@example.com");

        var adminToken = await CreateAdminAndLoginAsync("account.admin3@example.com", "AdminPassw0rd!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _client.PutAsJsonAsync($"/api/admin/users/{targetUserId}/role", new AssignRoleRequest("Bartender", "staffing up"));
        await _client.PutAsJsonAsync($"/api/staff-pins/{targetUserId}", new SetPinRequest("246810"));

        // Reason is mandatory — no silent deactivation.
        var noReason = await _client.PostAsJsonAsync($"/api/admin/users/{targetUserId}/deactivate",
            new AccountActionRequest(""));
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);

        var deactivated = await _client.PostAsJsonAsync($"/api/admin/users/{targetUserId}/deactivate",
            new AccountActionRequest("policy violation"));
        Assert.Equal(HttpStatusCode.NoContent, deactivated.StatusCode);

        var listAfterDeactivate = await _client.GetFromJsonAsync<List<AdminUserResponse>>("/api/admin/users");
        var rowAfterDeactivate = Assert.Single(listAfterDeactivate!, u => u.Id == targetUserId);
        Assert.False(rowAfterDeactivate.IsActive);
        Assert.False(rowAfterDeactivate.HasActivePin);

        // The account can no longer log in at all.
        var blockedLogin = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("account.flow@example.com", "Passw0rd!"));
        Assert.Equal(HttpStatusCode.Unauthorized, blockedLogin.StatusCode);

        var reactivated = await _client.PostAsJsonAsync($"/api/admin/users/{targetUserId}/reactivate",
            new AccountActionRequest("appeal approved"));
        Assert.Equal(HttpStatusCode.NoContent, reactivated.StatusCode);

        var restoredLogin = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("account.flow@example.com", "Passw0rd!"));
        Assert.Equal(HttpStatusCode.OK, restoredLogin.StatusCode);

        // Reactivating the account does not silently restore the PIN.
        var listAfterReactivate = await _client.GetFromJsonAsync<List<AdminUserResponse>>("/api/admin/users");
        var rowAfterReactivate = Assert.Single(listAfterReactivate!, u => u.Id == targetUserId);
        Assert.True(rowAfterReactivate.IsActive);
        Assert.False(rowAfterReactivate.HasActivePin);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var deactivateAudit = Assert.Single(context.AdminAudits, a => a.EntityId == targetUserId && a.Action == "Deactivate");
        Assert.Equal("policy violation", deactivateAudit.Reason);
        var reactivateAudit = Assert.Single(context.AdminAudits, a => a.EntityId == targetUserId && a.Action == "Reactivate");
        Assert.Equal("appeal approved", reactivateAudit.Reason);
    }

    [Fact]
    public async Task InviteEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/users/invite-bartender", new InviteBartenderRequest("new@example.com"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InviteEndpoint_WithCustomerOrBartenderToken_ReturnsForbidden()
    {
        var customerToken = await RegisterCustomerAsync("invite.customer@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        var asCustomer = await _client.PostAsJsonAsync("/api/admin/users/invite-bartender", new InviteBartenderRequest("new@example.com"));
        Assert.Equal(HttpStatusCode.Forbidden, asCustomer.StatusCode);

        var bartenderToken = await LoginAsync("bartender@example.com", "Bartender1!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bartenderToken);
        var asBartender = await _client.PostAsJsonAsync("/api/admin/users/invite-bartender", new InviteBartenderRequest("new@example.com"));
        Assert.Equal(HttpStatusCode.Forbidden, asBartender.StatusCode);
    }

    [Fact]
    public async Task InviteEndpoint_ExistingEmail_ReturnsConflict()
    {
        await RegisterCustomerAsync("invite.existing@example.com");
        var adminToken = await CreateAdminAndLoginAsync("invite.admin1@example.com", "AdminPassw0rd!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.PostAsJsonAsync("/api/admin/users/invite-bartender",
            new InviteBartenderRequest("invite.existing@example.com"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task InviteFlow_CreatesBartenderAccount_AndSetPasswordLinkActuallyLogsIn()
    {
        var adminToken = await CreateAdminAndLoginAsync("invite.admin2@example.com", "AdminPassw0rd!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        const string newHireEmail = "invite.newhire@example.com";
        var invited = await _client.PostAsJsonAsync("/api/admin/users/invite-bartender", new InviteBartenderRequest(newHireEmail));
        Assert.Equal(HttpStatusCode.OK, invited.StatusCode);
        var created = await invited.Content.ReadFromJsonAsync<AdminUserResponse>();
        Assert.Equal(newHireEmail, created!.Email);
        Assert.Equal("Bartender", created.Role);

        var sent = Assert.Single(_factory.EmailSender.SentEmails, e => e.ToEmail == newHireEmail);
        var token = ExtractTokenFromSetPasswordEmail(sent);

        // The invite link reuses the existing reset-password endpoint to let the new
        // hire set their first password, then they can log in as a Bartender.
        _client.DefaultRequestHeaders.Authorization = null;
        var setPassword = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new ResetPasswordRequest(newHireEmail, token, "NewHirePassw0rd!"));
        Assert.Equal(HttpStatusCode.OK, setPassword.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(newHireEmail, "NewHirePassw0rd!"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var audit = Assert.Single(context.AdminAudits, a => a.EntityId == created.Id);
        Assert.Equal("Invite", audit.Action);
        Assert.Equal("User", audit.EntityType);
        Assert.Null(audit.BeforeSnapshot);
    }

    private static string ExtractTokenFromSetPasswordEmail(FakeEmailSender.SentEmail email)
    {
        var tokenParam = email.Body.Split("token=").Last().Split('\n', ' ').First();
        return Uri.UnescapeDataString(tokenParam);
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
