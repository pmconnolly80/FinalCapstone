using BeerApi.Data;
using BeerApi.Models;
using BeerApi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BeerApi.Tests.Services;

[Collection("WebApplicationFactory")]
public class AccountDeletionServiceTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();

    public AccountDeletionServiceTests()
    {
        _factory.CreateClient();
    }

    [Fact]
    public async Task AnonymizeAsync_ExistingLinkedAccount_ScrubsIdentityAndRemovesLogin()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "fb.deleteme@example.com", Email = "fb.deleteme@example.com", EmailConfirmed = true };
        await userManager.CreateAsync(user);
        await userManager.AddLoginAsync(user, new UserLoginInfo("Facebook", "fb-delete-key", "Facebook"));

        var service = scope.ServiceProvider.GetRequiredService<IAccountDeletionService>();
        var confirmationCode = await service.AnonymizeAsync("Facebook", "fb-delete-key");

        Assert.False(string.IsNullOrWhiteSpace(confirmationCode));

        var reloaded = await userManager.FindByIdAsync(user.Id);
        Assert.NotNull(reloaded);
        Assert.NotEqual("fb.deleteme@example.com", reloaded!.Email);
        Assert.Contains(user.Id, reloaded.Email);
        Assert.False(reloaded.EmailConfirmed);

        var logins = await userManager.GetLoginsAsync(reloaded);
        Assert.Empty(logins);
    }

    [Fact]
    public async Task AnonymizeAsync_ExistingAccountWithPassword_RemovesPassword()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "fb.withpassword@example.com", Email = "fb.withpassword@example.com" };
        await userManager.CreateAsync(user, "Passw0rd!");
        await userManager.AddLoginAsync(user, new UserLoginInfo("Facebook", "fb-password-key", "Facebook"));

        var service = scope.ServiceProvider.GetRequiredService<IAccountDeletionService>();
        await service.AnonymizeAsync("Facebook", "fb-password-key");

        var reloaded = await userManager.FindByIdAsync(user.Id);
        Assert.False(await userManager.HasPasswordAsync(reloaded!));
    }

    [Fact]
    public async Task AnonymizeAsync_NoMatchingAccount_StillReturnsConfirmationCode()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAccountDeletionService>();

        var confirmationCode = await service.AnonymizeAsync("Facebook", "no-such-key");

        Assert.False(string.IsNullOrWhiteSpace(confirmationCode));
    }

    [Fact]
    public async Task AnonymizeAsync_PreservesBeerConfirmations_ForTheTavernsLedger()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new ApplicationUser { UserName = "fb.withhistory@example.com", Email = "fb.withhistory@example.com" };
        await userManager.CreateAsync(user);
        await userManager.AddLoginAsync(user, new UserLoginInfo("Facebook", "fb-history-key", "Facebook"));

        var beer = db.Beers.First();
        var tavern = db.Taverns.First();
        db.BeerConfirmations.Add(new BeerConfirmation
        {
            CustomerId = user.Id,
            BeerId = beer.Id,
            TavernId = tavern.Id,
            ConfirmedByUserId = user.Id,
        });
        await db.SaveChangesAsync();

        var service = scope.ServiceProvider.GetRequiredService<IAccountDeletionService>();
        await service.AnonymizeAsync("Facebook", "fb-history-key");

        var confirmationStillExists = await db.BeerConfirmations.AnyAsync(c => c.CustomerId == user.Id && c.BeerId == beer.Id);
        Assert.True(confirmationStillExists);
    }

    public void Dispose() => _factory.Dispose();
}
