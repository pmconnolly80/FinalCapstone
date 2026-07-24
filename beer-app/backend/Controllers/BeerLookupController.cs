using System.Security.Claims;
using BeerApi.Data;
using BeerApi.Models;
using BeerApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Controllers;

// #72: customer-facing "look up any beer" search — distinct from BeersController's
// search of the tavern's own ~200-beer list. Reuses the already-registered
// ICatalogBeerService/IBreweryLookupService (previously Admin-only, via
// CatalogBeerController/BreweriesController); both already degrade to empty results on
// any external failure. Signed-in-only and rate-limited since Catalog.beer is a real
// API-keyed, cost-sensitive dependency (see Program.cs's "PerUserExternalSearch" policy).
[ApiController]
[Route("api/beer-lookup")]
[Authorize]
public class BeerLookupController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICatalogBeerService _catalogBeer;
    private readonly IBreweryLookupService _breweryLookup;

    public BeerLookupController(
        ApplicationDbContext context, ICatalogBeerService catalogBeer, IBreweryLookupService breweryLookup)
    {
        _context = context;
        _catalogBeer = catalogBeer;
        _breweryLookup = breweryLookup;
    }

    [HttpGet("search")]
    [EnableRateLimiting("PerUserExternalSearch")]
    public async Task<ActionResult<BeerLookupResponse>> Search([FromQuery] string? query)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (customerId == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return new BeerLookupResponse(Array.Empty<CatalogBeerResult>(), Array.Empty<BreweryInfo>());
        }

        var beers = await _catalogBeer.SearchAsync(query);
        var breweries = await _breweryLookup.SearchBreweriesAsync(query);

        var term = query.Trim().ToLower();
        var matchedTavernCatalog = await _context.Beers.AnyAsync(b =>
            b.Name.ToLower().Contains(term) ||
            b.Brewery.ToLower().Contains(term) ||
            b.Style.ToLower().Contains(term));

        _context.ExternalSearchLogs.Add(new ExternalSearchLog
        {
            CustomerId = customerId,
            Query = query.Trim(),
            MatchedTavernCatalog = matchedTavernCatalog,
        });
        await _context.SaveChangesAsync();

        return new BeerLookupResponse(beers, breweries);
    }
}

public record BeerLookupResponse(IReadOnlyList<CatalogBeerResult> Beers, IReadOnlyList<BreweryInfo> Breweries);
