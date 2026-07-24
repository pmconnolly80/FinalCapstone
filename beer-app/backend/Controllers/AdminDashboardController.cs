using BeerApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Controllers;

// #59: closes Sprint 5 — one summary endpoint for the Admin Dashboard rather than
// stitching together 3-4 client-side counts from other screens' list endpoints (none of
// which have a cheap count-only path, and "active members" isn't computable from any
// existing endpoint at all). Real COUNT/COUNT(DISTINCT) queries throughout.
[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminDashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    // #78: "most/least-confirmed beers" — a cheap first slice of the full "beer
    // intelligence" Terri actually wants (PERSONAS_AND_USAGE.md's "Weekly ritual"),
    // pulled forward ahead of the fuller Owner Analytics screen. A simple GROUP BY
    // over existing BeerConfirmation rows, no new schema.
    public const int TopN = 5;

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary() =>
        await ComputeSummaryAsync(_context, DateTime.UtcNow);

    [HttpGet("beer-confirmations")]
    public async Task<ActionResult<BeerConfirmationCountsResponse>> GetBeerConfirmationCounts() =>
        await ComputeBeerConfirmationCountsAsync(_context);

    // public + explicit `now` param (same lesson as #58's AdminAnomaliesController) —
    // unit tests call this directly with a fixed reference time instead of going
    // through the HTTP action, so "today"/"last 30 days" boundaries aren't flaky.
    public static async Task<DashboardSummaryResponse> ComputeSummaryAsync(ApplicationDbContext context, DateTime now)
    {
        var todayStart = now.Date;
        var activeWindowStart = now.AddDays(-30);

        var totalBeers = await context.Beers.CountAsync();
        var confirmationsToday = await context.BeerConfirmations.CountAsync(c => c.ConfirmedAt >= todayStart);
        var activeMembers = await context.BeerConfirmations
            .Where(c => c.ConfirmedAt >= activeWindowStart)
            .Select(c => c.CustomerId)
            .Distinct()
            .CountAsync();
        var mugsAwarded = await context.MugAwards.CountAsync();

        return new DashboardSummaryResponse(totalBeers, confirmationsToday, activeMembers, mugsAwarded);
    }

    // Beers with zero confirmations matter just as much as the busiest ones — "the
    // stout nobody's ordered in two months" (PERSONAS_AND_USAGE.md) — so this counts
    // every beer, not just ones with at least one BeerConfirmation row, rather than a
    // plain GroupBy over BeerConfirmations which would silently omit them.
    public static async Task<BeerConfirmationCountsResponse> ComputeBeerConfirmationCountsAsync(ApplicationDbContext context)
    {
        var beers = await context.Beers.Select(b => new { b.Id, b.Name }).ToListAsync();
        var confirmedCounts = await context.BeerConfirmations
            .GroupBy(c => c.BeerId)
            .Select(g => new { BeerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.BeerId, g => g.Count);

        var counts = beers
            .Select(b => new BeerConfirmationCount(b.Id, b.Name, confirmedCounts.GetValueOrDefault(b.Id, 0)))
            .ToList();

        var mostConfirmed = counts
            .OrderByDescending(c => c.ConfirmedCount)
            .ThenBy(c => c.Name)
            .Take(TopN)
            .ToList();
        var leastConfirmed = counts
            .OrderBy(c => c.ConfirmedCount)
            .ThenBy(c => c.Name)
            .Take(TopN)
            .ToList();

        return new BeerConfirmationCountsResponse(mostConfirmed, leastConfirmed);
    }
}

public record DashboardSummaryResponse(int TotalBeers, int ConfirmationsToday, int ActiveMembers, int MugsAwarded);
public record BeerConfirmationCount(int BeerId, string Name, int ConfirmedCount);
public record BeerConfirmationCountsResponse(IReadOnlyList<BeerConfirmationCount> MostConfirmed, IReadOnlyList<BeerConfirmationCount> LeastConfirmed);
