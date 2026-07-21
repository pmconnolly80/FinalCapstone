using System.Security.Claims;
using BeerApi.Data;
using BeerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BeersController : ControllerBase
{
    // The tavern's list runs to ~200 beers, so an unpaginated default keeps today's
    // catalog on one page; real paging kicks in once a caller asks for it.
    public const int DefaultPageSize = 200;
    public const int MaxPageSize = 500;

    private readonly ApplicationDbContext _context;

    public BeersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<BeerSearchResponse>> GetBeers(
        string? search = null, string? availability = null, string? hadStatus = null,
        int page = 1, int pageSize = DefaultPageSize)
    {
        var query = _context.Beers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(b =>
                b.Name.ToLower().Contains(term) ||
                b.Brewery.ToLower().Contains(term) ||
                b.Style.ToLower().Contains(term));
        }

        if (string.IsNullOrWhiteSpace(availability))
        {
            // Default view: what's actually pourable right now. Out-of-stock/retired
            // beers still exist for history and the had/not-had filter below.
            query = query.Where(b => b.Availability == BeerAvailability.OnTap || b.Availability == BeerAvailability.Available);
        }
        else if (!string.Equals(availability, "all", StringComparison.OrdinalIgnoreCase))
        {
            if (!Enum.TryParse<BeerAvailability>(availability, ignoreCase: true, out var parsedAvailability))
            {
                return BadRequest($"Unknown availability '{availability}'.");
            }
            query = query.Where(b => b.Availability == parsedAvailability);
        }

        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var confirmedBeerIds = customerId == null
            ? new HashSet<int>()
            : (await _context.BeerConfirmations
                .Where(c => c.CustomerId == customerId)
                .Select(c => c.BeerId)
                .ToListAsync())
                .ToHashSet();

        if (!string.IsNullOrWhiteSpace(hadStatus))
        {
            if (customerId == null)
            {
                return Unauthorized();
            }

            if (string.Equals(hadStatus, "had", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(b => confirmedBeerIds.Contains(b.Id));
            }
            else if (string.Equals(hadStatus, "nothad", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(b => !confirmedBeerIds.Contains(b.Id));
            }
            else
            {
                return BadRequest($"Unknown hadStatus '{hadStatus}'.");
            }
        }

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var totalCount = await query.CountAsync();
        var beers = await query
            .OrderBy(b => b.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = beers
            .Select(b => new BeerSearchItem(
                b.Id, b.Name, b.Brewery, b.Style, b.Description, b.Availability, b.CreatedAt,
                confirmedBeerIds.Contains(b.Id)))
            .ToList();

        return new BeerSearchResponse(items, page, pageSize, totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Beer>> GetBeer(int id)
    {
        var beer = await _context.Beers.FindAsync(id);
        if (beer == null)
        {
            return NotFound();
        }

        return beer;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Beer>> PostBeer(Beer beer)
    {
        _context.Beers.Add(beer);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBeer), new { id = beer.Id }, beer);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> PutBeer(int id, Beer beer)
    {
        if (id != beer.Id)
        {
            return BadRequest();
        }

        _context.Entry(beer).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBeer(int id)
    {
        var beer = await _context.Beers.FindAsync(id);
        if (beer == null)
        {
            return NotFound();
        }

        _context.Beers.Remove(beer);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record BeerSearchItem(
    int Id,
    string Name,
    string Brewery,
    string Style,
    string? Description,
    BeerAvailability Availability,
    DateTime CreatedAt,
    bool Confirmed);

public record BeerSearchResponse(
    IReadOnlyList<BeerSearchItem> Items,
    int Page,
    int PageSize,
    int TotalCount);
