using BeerApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeerApi.Controllers;

// Admin-only: Catalog.beer pre-fill search (#31) — a typing-saver for the admin add/edit
// form, same gating rationale as brewery autocomplete (#30).
[ApiController]
[Route("api/catalog-beer")]
[Authorize(Roles = "Admin")]
public class CatalogBeerController : ControllerBase
{
    private readonly ICatalogBeerService _catalogBeer;

    public CatalogBeerController(ICatalogBeerService catalogBeer)
    {
        _catalogBeer = catalogBeer;
    }

    [HttpGet("search")]
    public async Task<IReadOnlyList<CatalogBeerResult>> Search([FromQuery] string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<CatalogBeerResult>();
        }

        return await _catalogBeer.SearchAsync(query);
    }
}
