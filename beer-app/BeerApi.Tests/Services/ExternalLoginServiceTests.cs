using BeerApi.Models;
using BeerApi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BeerApi.Tests.Services;

// Exercises the real Identity/EF stack (via the same WebApplicationFactory the
// controller-level integration tests use) rather than a real browser OAuth handshake —
// the link-or-create-by-verified-email rule is this app's own logic and the part these
// tests can meaningfully cover; the outer challenge/callback wiring against each live
// provider needs manual verification against real developer-console credentials.
[Collection("WebApplicationFactory")]
public class ExternalLoginServiceTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();

    public ExternalLoginServiceTests()
    {
        _factory.CreateClient(); // forces host startup (roles seeded, etc.)
    }

    private IExternalLoginService CreateService(out IServiceScope scope)
    {
        scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IExternalLoginService>();
    }

    [Fact]
    public async Task ProcessLoginAsync_NoExistingAccount_CreatesNewAccount_WithCustomerRoleAndLinkedLogin()
    {
        var service = CreateService(out var scope);
        using (scope)
        {
            var result = await service.ProcessLoginAsync("Google", "google-key-1", "new.google.user@example.com", "New User");

            Assert.True(result.IsNewAccount);
            Assert.Equal("new.google.user@example.com", result.User.Email);
            Assert.True(result.User.EmailConfirmed);

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True(await userManager.IsInRoleAsync(result.User, "Customer"));

            var logins = await userManager.GetLoginsAsync(result.User);
            Assert.Contains(logins, l => l.LoginProvider == "Google" && l.ProviderKey == "google-key-1");
        }
    }

    [Fact]
    public async Task ProcessLoginAsync_ExistingPasswordAccountWithSameEmail_LinksLogin_WithoutDuplicatingAccount()
    {
        var service = CreateService(out var scope);
        using (scope)
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var existingUser = new ApplicationUser { UserName = "existing.password.user@example.com", Email = "existing.password.user@example.com" };
            await userManager.CreateAsync(existingUser, "Passw0rd!");

            var result = await service.ProcessLoginAsync("Google", "google-key-2", "existing.password.user@example.com", "Existing User");

            Assert.False(result.IsNewAccount);
            Assert.Equal(existingUser.Id, result.User.Id);

            var logins = await userManager.GetLoginsAsync(existingUser);
            Assert.Contains(logins, l => l.LoginProvider == "Google" && l.ProviderKey == "google-key-2");

            var allUsersWithEmail = userManager.Users.Where(u => u.Email == "existing.password.user@example.com").ToList();
            Assert.Single(allUsersWithEmail);
        }
    }

    [Fact]
    public async Task ProcessLoginAsync_AlreadyLinkedLogin_ReturnsSameAccount_WithoutCreatingDuplicateLogin()
    {
        var service = CreateService(out var scope);
        using (scope)
        {
            var first = await service.ProcessLoginAsync("Google", "google-key-3", "repeat.user@example.com", "Repeat User");
            var second = await service.ProcessLoginAsync("Google", "google-key-3", "repeat.user@example.com", "Repeat User");

            Assert.True(first.IsNewAccount);
            Assert.False(second.IsNewAccount);
            Assert.Equal(first.User.Id, second.User.Id);

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var logins = await userManager.GetLoginsAsync(second.User);
            Assert.Single(logins, l => l.LoginProvider == "Google" && l.ProviderKey == "google-key-3");
        }
    }

    public void Dispose() => _factory.Dispose();
}
