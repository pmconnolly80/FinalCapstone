using BeerApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Controllers;

// #83: what #72 logs ("look up any beer" searches with no match in the tavern's own
// list) has nowhere to be seen — this surfaces it, aggregated by frequency, as an
// ordering-decision signal for the owner. Computed on demand like AdminAnomaliesController
// (#58) — no new background job, same explicit-`now`-parameter testability pattern.
[ApiController]
[Route("api/admin/external-search-demand")]
[Authorize(Roles = "Admin")]
public class AdminExternalSearchController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminExternalSearchController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DemandItem>>> GetDemand(int sinceDays = 30, int topN = 20)
    {
        return await ComputeDemandAsync(_context, DateTime.UtcNow, sinceDays, topN);
    }

    public static async Task<List<DemandItem>> ComputeDemandAsync(
        ApplicationDbContext context, DateTime now, int sinceDays = 30, int topN = 20)
    {
        var windowStart = now.AddDays(-sinceDays);

        var rows = await context.ExternalSearchLogs
            .Where(l => !l.MatchedTavernCatalog && l.CreatedAt >= windowStart)
            .ToListAsync();

        return rows
            .GroupBy(l => l.Query.Trim().ToLowerInvariant())
            .Select(g => new DemandItem(g.Key, g.Count(), g.Max(l => l.CreatedAt)))
            .OrderByDescending(item => item.Count)
            .Take(topN)
            .ToList();
    }
}

public record DemandItem(string Query, int Count, DateTime LastSearchedAt);
