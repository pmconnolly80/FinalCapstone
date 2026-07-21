namespace BeerApi.Services;

// Beer-level pre-fill data (#31) — Catalog.beer is community-contributed (CC BY 4.0), a
// typing-saver for the admin to verify, never a source of truth. Class is "Ale"/"Lager"
// to match our BeerClass enum casing.
public record CatalogBeerResult(
    string Id,
    string Name,
    string? Style,
    string? StyleFamily,
    string? Class,
    string? Description,
    double? Abv,
    int? Ibu,
    bool CbVerified,
    string? BrewerName);

public interface ICatalogBeerService
{
    Task<IReadOnlyList<CatalogBeerResult>> SearchAsync(string query);
}
