using System.Security.Claims;
using BeerApi.Data;
using BeerApi.Models;
using BeerApi.Services;
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
    private readonly IBreweryLookupService _breweryLookup;

    public BeersController(ApplicationDbContext context, IBreweryLookupService breweryLookup)
    {
        _context = context;
        _breweryLookup = breweryLookup;
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
    public async Task<ActionResult<BeerDetailResponse>> GetBeer(int id)
    {
        var beer = await _context.Beers.FindAsync(id);
        if (beer == null)
        {
            return NotFound();
        }

        var breweryInfo = beer.ObdbBreweryId == null
            ? null
            : await _breweryLookup.GetBreweryAsync(beer.ObdbBreweryId);

        return new BeerDetailResponse(
            beer.Id, beer.Name, beer.Brewery, beer.Style, beer.Description, beer.Availability,
            beer.Abv, beer.Ibu, beer.StyleFamily, beer.Class, beer.ObdbBreweryId, beer.CreatedAt,
            breweryInfo);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Beer>> PostBeer(Beer beer)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (adminId == null)
        {
            return Unauthorized();
        }

        _context.Beers.Add(beer);
        await _context.SaveChangesAsync();

        // #58: attributes bulk-add anomalies to an admin — until now nothing recorded
        // who created a beer at all, only edits/deletes (#56).
        _context.AdminAudits.Add(new AdminAudit
        {
            AdminUserId = adminId,
            EntityType = "Beer",
            EntityId = beer.Id.ToString(),
            Action = "Create",
            BeforeSnapshot = null,
            AfterSnapshot = $"{beer.Name} ({beer.Brewery})",
            Reason = string.Empty,
        });
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

        var existing = await _context.Beers.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (adminId == null)
        {
            return Unauthorized();
        }

        var (before, after) = DescribeBeerDiff(existing, beer);

        _context.Entry(beer).State = EntityState.Modified;

        if (before.Length > 0)
        {
            _context.AdminAudits.Add(new AdminAudit
            {
                AdminUserId = adminId,
                EntityType = "Beer",
                EntityId = id.ToString(),
                Action = "Edit",
                BeforeSnapshot = before,
                AfterSnapshot = after,
                Reason = string.Empty,
            });
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBeer(int id, string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return BadRequest(new { message = "A reason is required to delete a beer." });
        }

        var beer = await _context.Beers.FindAsync(id);
        if (beer == null)
        {
            return NotFound();
        }

        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (adminId == null)
        {
            return Unauthorized();
        }

        // BeerConfirmation.BeerId is a restrict-on-delete FK (ApplicationDbContext), so
        // deleting a beer that's already been confirmed would otherwise fail deep inside
        // SaveChangesAsync with an unhandled DbUpdateException (a bare 500 with a raw DB
        // stack trace, confirmed live 2026-07-23 — see CLAUDE.md). Checking up front gives
        // a clean, actionable 409 instead, and keeps the check testable against the
        // InMemory provider, which doesn't enforce the FK the way Postgres does.
        var hasConfirmations = await _context.BeerConfirmations.AnyAsync(c => c.BeerId == id);
        if (hasConfirmations)
        {
            return Conflict(new
            {
                message = "This beer has already been confirmed by at least one customer and can't be deleted. Void those confirmations first, or mark it Retired instead."
            });
        }

        _context.AdminAudits.Add(new AdminAudit
        {
            AdminUserId = adminId,
            EntityType = "Beer",
            EntityId = id.ToString(),
            Action = "Delete",
            BeforeSnapshot = $"{beer.Name} ({beer.Brewery})",
            AfterSnapshot = null,
            Reason = reason.Trim(),
        });

        _context.Beers.Remove(beer);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/availability")]
    public async Task<IActionResult> UpdateAvailability(int id, UpdateAvailabilityRequest request)
    {
        var beer = await _context.Beers.FindAsync(id);
        if (beer == null)
        {
            return NotFound();
        }

        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (adminId == null)
        {
            return Unauthorized();
        }

        if (beer.Availability == request.Availability)
        {
            return NoContent();
        }

        var previous = beer.Availability;
        beer.Availability = request.Availability;

        _context.AdminAudits.Add(new AdminAudit
        {
            AdminUserId = adminId,
            EntityType = "Beer",
            EntityId = id.ToString(),
            Action = "AvailabilityChange",
            BeforeSnapshot = previous.ToString(),
            AfterSnapshot = request.Availability.ToString(),
            Reason = string.Empty,
        });

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // #56: a beer edit touches ~10 fields, but AdminAudit's BeforeSnapshot/AfterSnapshot
    // are plain strings (#53/#54 only ever stored one scalar each) — rather than this
    // codebase's first JSON-blob snapshot, this captures only what actually changed as
    // short readable text, same tone as the rest of the audit trail.
    private static (string Before, string After) DescribeBeerDiff(Beer before, Beer after)
    {
        var changes = new (string Label, string? Old, string? New)[]
        {
            ("Name", before.Name, after.Name),
            ("Brewery", before.Brewery, after.Brewery),
            ("Style", before.Style, after.Style),
            ("Description", before.Description, after.Description),
            ("Availability", before.Availability.ToString(), after.Availability.ToString()),
            ("Abv", before.Abv?.ToString(), after.Abv?.ToString()),
            ("Ibu", before.Ibu?.ToString(), after.Ibu?.ToString()),
            ("StyleFamily", before.StyleFamily, after.StyleFamily),
            ("Class", before.Class?.ToString(), after.Class?.ToString()),
            ("ObdbBreweryId", before.ObdbBreweryId, after.ObdbBreweryId),
        }.Where(c => c.Old != c.New).ToArray();

        return (
            string.Join("; ", changes.Select(c => $"{c.Label}: {c.Old ?? "(none)"}")),
            string.Join("; ", changes.Select(c => $"{c.Label}: {c.New ?? "(none)"}")));
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

public record UpdateAvailabilityRequest(BeerAvailability Availability);

public record BeerDetailResponse(
    int Id,
    string Name,
    string Brewery,
    string Style,
    string? Description,
    BeerAvailability Availability,
    double? Abv,
    int? Ibu,
    string? StyleFamily,
    BeerClass? Class,
    string? ObdbBreweryId,
    DateTime CreatedAt,
    BreweryInfo? BreweryInfo);
