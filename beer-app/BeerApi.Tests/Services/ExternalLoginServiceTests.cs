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

    [Fact]
    public async Task ProcessLoginAsync_AppleProvider_CreatesAccount_SameAsAnyOtherProvider()
    {
        var service = CreateService(out var scope);
        using (scope)
        {
            var result = await service.ProcessLoginAsync("Apple", "apple-key-1", "new.apple.user@example.com", null);

            Assert.True(result.IsNewAccount);

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var logins = await userManager.GetLoginsAsync(result.User);
            Assert.Contains(logins, l => l.LoginProvider == "Apple" && l.ProviderKey == "apple-key-1");
        }
    }

    // #45's documented relay-email caveat: this isn't a bug to fix, it's the expected
    // consequence of matching purely on email — see AuthController.IsEmailVerified's
    // comment for why resolving it needs account-linking UI (#46), not smarter matching.
    [Fact]
    public async Task ProcessLoginAsync_AppleRelayEmail_DiffersFromExistingRealEmail_CreatesSeparateAccount_DoesNotAutoLink()
    {
        var service = CreateService(out var scope);
        using (scope)
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var passwordUser = new ApplicationUser { UserName = "real.address@example.com", Email = "real.address@example.com" };
            await userManager.CreateAsync(passwordUser, "Passw0rd!");

            var result = await service.ProcessLoginAsync(
                "Apple", "apple-relay-key", "abc123@privaterelay.appleid.com", "Relay User");

            Assert.True(result.IsNewAccount);
            Assert.NotEqual(passwordUser.Id, result.User.Id);
        }
    }

    [Fact]
    public async Task LinkAdditionalProviderAsync_NewProviderForExistingUser_LinksSuccessfully()
    {
        var service = CreateService(out var scope);
        using (scope)
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = "link.target@example.com", Email = "link.target@example.com" };
            await userManager.CreateAsync(user, "Passw0rd!");

            var result = await service.LinkAdditionalProviderAsync(user, "Facebook", "fb-link-key", "Link Target");

            Assert.True(result.Succeeded);
            Assert.Null(result.Error);

            var logins = await userManager.GetLoginsAsync(user);
            Assert.Contains(logins, l => l.LoginProvider == "Facebook" && l.ProviderKey == "fb-link-key");
        }
    }

    [Fact]
    public async Task LinkAdditionalProviderAsync_AlreadyLinkedToSameUser_IsIdempotent()
    {
        var service = CreateService(out var scope);
        using (scope)
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = "relink.self@example.com", Email = "relink.self@example.com" };
            await userManager.CreateAsync(user, "Passw0rd!");
            await userManager.AddLoginAsync(user, new UserLoginInfo("Facebook", "fb-relink-key", "Facebook"));

            var result = await service.LinkAdditionalProviderAsync(user, "Facebook", "fb-relink-key", "Relink Self");

            Assert.True(result.Succeeded);

            var logins = await userManager.GetLoginsAsync(user);
            Assert.Single(logins, l => l.LoginProvider == "Facebook" && l.ProviderKey == "fb-relink-key");
        }
    }

    [Fact]
    public async Task LinkAdditionalProviderAsync_AlreadyLinkedToDifferentUser_Fails_WithoutStealingTheLogin()
    {
        var service = CreateService(out var scope);
        using (scope)
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var owner = new ApplicationUser { UserName = "owner@example.com", Email = "owner@example.com" };
            await userManager.CreateAsync(owner, "Passw0rd!");
            await userManager.AddLoginAsync(owner, new UserLoginInfo("Facebook", "fb-contested-key", "Facebook"));

            var wouldBeThief = new ApplicationUser { UserName = "attacker@example.com", Email = "attacker@example.com" };
            await userManager.CreateAsync(wouldBeThief, "Passw0rd!");

            var result = await service.LinkAdditionalProviderAsync(wouldBeThief, "Facebook", "fb-contested-key", "Attacker");

            Assert.False(result.Succeeded);
            Assert.Equal("already_linked_to_another_account", result.Error);

            var ownerLogins = await userManager.GetLoginsAsync(owner);
            Assert.Contains(ownerLogins, l => l.LoginProvider == "Facebook" && l.ProviderKey == "fb-contested-key");
            var thiefLogins = await userManager.GetLoginsAsync(wouldBeThief);
            Assert.Empty(thiefLogins);
        }
    }

    public void Dispose() => _factory.Dispose();
}
