namespace BeerApi.Services;

public interface IAccountDeletionService
{
    // Always returns a confirmation code, whether or not a matching account existed —
    // mirrors the login/reset endpoints' account-enumeration avoidance.
    Task<string> AnonymizeAsync(string loginProvider, string providerKey);
}
