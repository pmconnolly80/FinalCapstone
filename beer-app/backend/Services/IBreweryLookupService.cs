namespace BeerApi.Services;

public record BreweryInfo(string Id, string Name, string? BreweryType, string? City, string? State, string? WebsiteUrl);

public interface IBreweryLookupService
{
    Task<BreweryInfo?> GetBreweryAsync(string breweryId);
    Task<IReadOnlyList<BreweryInfo>> SearchBreweriesAsync(string query);
}
