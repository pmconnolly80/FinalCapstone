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

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary() =>
        await ComputeSummaryAsync(_context, DateTime.UtcNow);

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
}

public record DashboardSummaryResponse(int TotalBeers, int ConfirmationsToday, int ActiveMembers, int MugsAwarded);
