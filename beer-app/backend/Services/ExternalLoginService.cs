using BeerApi.Models;
using Microsoft.AspNetCore.Identity;

namespace BeerApi.Services;

public class ExternalLoginService : IExternalLoginService
{
    private const string DefaultRole = "Customer";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public ExternalLoginService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<ExternalLoginResult> ProcessLoginAsync(string loginProvider, string providerKey, string email, string? displayName)
    {
        var existingLogin = await _userManager.FindByLoginAsync(loginProvider, providerKey);
        if (existingLogin != null)
        {
            return new ExternalLoginResult(existingLogin, false);
        }

        var existingByEmail = await _userManager.FindByEmailAsync(email);
        if (existingByEmail != null)
        {
            await LinkLoginAsync(existingByEmail, loginProvider, providerKey, displayName);
            return new ExternalLoginResult(existingByEmail, false);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            // The provider already verified this address (checked by the caller before
            // reaching here) — Identity's own EmailConfirmed gate would otherwise block
            // password-reset-style flows for an account that has no password to begin with.
            EmailConfirmed = true,
        };

        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", createResult.Errors.Select(e => e.Description)));
        }

        if (!await _roleManager.RoleExistsAsync(DefaultRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(DefaultRole));
        }
        await _userManager.AddToRoleAsync(user, DefaultRole);

        await LinkLoginAsync(user, loginProvider, providerKey, displayName);
        return new ExternalLoginResult(user, true);
    }

    public async Task<LinkExternalLoginResult> LinkAdditionalProviderAsync(ApplicationUser user, string loginProvider, string providerKey, string? displayName)
    {
        var existingLogin = await _userManager.FindByLoginAsync(loginProvider, providerKey);
        if (existingLogin != null)
        {
            return existingLogin.Id == user.Id
                ? new LinkExternalLoginResult(true, null)
                : new LinkExternalLoginResult(false, "already_linked_to_another_account");
        }

        var loginInfo = new UserLoginInfo(loginProvider, providerKey, displayName ?? loginProvider);
        var result = await _userManager.AddLoginAsync(user, loginInfo);
        return result.Succeeded
            ? new LinkExternalLoginResult(true, null)
            : new LinkExternalLoginResult(false, string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    private async Task LinkLoginAsync(ApplicationUser user, string loginProvider, string providerKey, string? displayName)
    {
        var loginInfo = new UserLoginInfo(loginProvider, providerKey, displayName ?? loginProvider);
        var result = await _userManager.AddLoginAsync(user, loginInfo);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }
}
