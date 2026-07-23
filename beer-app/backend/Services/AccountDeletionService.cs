using BeerApi.Models;
using Microsoft.AspNetCore.Identity;

namespace BeerApi.Services;

// Anonymizes rather than hard-deletes: BeerConfirmation/MugAward/ConfirmationAudit rows
// key off CustomerId (a plain string FK to AspNetUsers.Id, see BeerConfirmation.cs) with
// no cascade path, and the tavern's confirmed-beer ledger is the whole point of the
// app — deleting the row out from under it would either orphan or destroy that history.
// Scrubbing the identifying fields and unlinking every login satisfies "delete my data"
// the same way removing your name from a bar's paper punch-card would.
public class AccountDeletionService : IAccountDeletionService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountDeletionService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<string> AnonymizeAsync(string loginProvider, string providerKey)
    {
        var confirmationCode = Guid.NewGuid().ToString("N");

        var user = await _userManager.FindByLoginAsync(loginProvider, providerKey);
        if (user == null)
        {
            return confirmationCode;
        }

        var logins = await _userManager.GetLoginsAsync(user);
        foreach (var login in logins)
        {
            await _userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
        }

        if (await _userManager.HasPasswordAsync(user))
        {
            await _userManager.RemovePasswordAsync(user);
        }

        var anonymizedEmail = $"deleted-{user.Id}@deleted.local";
        await _userManager.SetUserNameAsync(user, anonymizedEmail);
        await _userManager.SetEmailAsync(user, anonymizedEmail);

        user.EmailConfirmed = false;
        user.MarketingConsent = false;
        user.PhoneNumber = null;
        await _userManager.UpdateAsync(user);

        return confirmationCode;
    }
}
