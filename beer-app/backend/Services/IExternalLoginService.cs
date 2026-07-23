using BeerApi.Models;

namespace BeerApi.Services;

public record ExternalLoginResult(ApplicationUser User, bool IsNewAccount);
public record LinkExternalLoginResult(bool Succeeded, string? Error);

// Shared by every external provider (#43 Google, #44 Facebook, #45 Apple) — the
// link-or-create-by-verified-email rule is identical across all three per
// TECHNICAL_ARCHITECTURE_PLAN.md §4.6; only how each provider proves the email is
// verified differs, which is the caller's job before invoking this.
public interface IExternalLoginService
{
    Task<ExternalLoginResult> ProcessLoginAsync(string loginProvider, string providerKey, string email, string? displayName);

    // #46: attaches an additional provider to an already-signed-in account (the
    // account-linking screen), distinct from ProcessLoginAsync's find-or-create-by-email
    // sign-in flow — the target account is already known, not resolved from the email.
    Task<LinkExternalLoginResult> LinkAdditionalProviderAsync(ApplicationUser user, string loginProvider, string providerKey, string? displayName);
}
