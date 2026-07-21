using BeerApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeerApi.Controllers;

// Admin-only: brewery autocomplete (#30) exists to save admins from typing brewery
// metadata by hand on the add/edit-beer form — no customer-facing use for this endpoint.
[ApiController]
[Route("api/breweries")]
[Authorize(Roles = "Admin")]
public class BreweriesController : ControllerBase
{
    private readonly IBreweryLookupService _breweryLookup;

    public BreweriesController(IBreweryLookupService breweryLookup)
    {
        _breweryLookup = breweryLookup;
    }

    [HttpGet("search")]
    public async Task<IReadOnlyList<BreweryInfo>> Search([FromQuery] string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<BreweryInfo>();
        }

        return await _breweryLookup.SearchBreweriesAsync(query);
    }
}
