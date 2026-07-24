using BeerApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Controllers;

// #58: informational-only anomaly detection, computed on demand from existing tables —
// no new tables, no background job (nothing in this issue's scope needs persisted
// state; the fuller "background job pipeline + push delivery" vision in
// TECHNICAL_ARCHITECTURE_PLAN.md §4.5 is a later, broader epic). Thresholds are
// config-driven (Anomalies:* in appsettings.json), same IConfiguration-direct-read
// pattern as CatalogBeer/Email — no options-class binding exists anywhere else in this
// codebase either.
[ApiController]
[Route("api/admin/anomalies")]
[Authorize(Roles = "Admin")]
public class AdminAnomaliesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AdminAnomaliesController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AnomalyResponse>>> GetAnomalies()
    {
        var now = DateTime.UtcNow;
        var anomalies = new List<AnomalyResponse>();
        anomalies.AddRange(await DetectBulkBeerAddAsync(_context, _configuration, now));
        anomalies.AddRange(await DetectConfirmationVelocityAsync(_context, _configuration, now));
        anomalies.AddRange(await DetectOffHoursActivityAsync(_context, _configuration, now));
        anomalies.AddRange(await DetectUnavailabilityReportsAsync(_context, _configuration, now));
        return anomalies.OrderByDescending(a => a.OccurredAt).ToList();
    }

    // public + an explicit `now` parameter (rather than reading DateTime.UtcNow
    // internally) so unit tests can call these directly with a fixed reference time —
    // bucket-boundary logic reading the real clock would make tests flaky depending on
    // when they happen to run.
    public static async Task<List<AnomalyResponse>> DetectBulkBeerAddAsync(
        ApplicationDbContext context, IConfiguration configuration, DateTime now)
    {
        var windowMinutes = configuration.GetValue("Anomalies:BulkBeerAdd:WindowMinutes", 60);
        var threshold = configuration.GetValue("Anomalies:BulkBeerAdd:Threshold", 10);
        var lookbackHours = configuration.GetValue("Anomalies:BulkBeerAdd:LookbackHours", 24);
        var windowStart = now.AddHours(-lookbackHours);

        var beers = await context.Beers
            .Where(b => b.CreatedAt >= windowStart)
            .Select(b => new { b.Id, b.CreatedAt })
            .ToListAsync();

        var results = new List<AnomalyResponse>();
        foreach (var bucket in beers.GroupBy(b => BucketIndex(b.CreatedAt, windowStart, windowMinutes)))
        {
            if (bucket.Count() < threshold)
            {
                continue;
            }

            // Compare against string EntityIds rather than int.Parse(a.EntityId) inside
            // the query — EF Core can't translate int.Parse in a LINQ predicate.
            var beerIdStrings = bucket.Select(b => b.Id.ToString()).ToList();
            var actorIds = await context.AdminAudits
                .Where(a => a.EntityType == "Beer" && a.Action == "Create" && beerIdStrings.Contains(a.EntityId))
                .Select(a => a.AdminUserId)
                .Distinct()
                .ToListAsync();
            var actorId = actorIds.Count == 1 ? actorIds[0] : null;
            var actorEmail = actorId == null
                ? null
                : await context.Users.Where(u => u.Id == actorId).Select(u => u.Email).FirstOrDefaultAsync();

            results.Add(new AnomalyResponse(
                "BulkBeerAdd",
                windowStart.AddMinutes(bucket.Key * windowMinutes),
                $"{bucket.Count()} beers added within {windowMinutes} minutes",
                actorId,
                actorEmail,
                "/admin/beers"));
        }
        return results;
    }

    public static async Task<List<AnomalyResponse>> DetectConfirmationVelocityAsync(
        ApplicationDbContext context, IConfiguration configuration, DateTime now)
    {
        var windowMinutes = configuration.GetValue("Anomalies:ConfirmationVelocity:WindowMinutes", 60);
        var baselineDays = configuration.GetValue("Anomalies:ConfirmationVelocity:BaselineDays", 7);
        var multiplier = configuration.GetValue("Anomalies:ConfirmationVelocity:Multiplier", 3.0);
        var minimumCount = configuration.GetValue("Anomalies:ConfirmationVelocity:MinimumCount", 5);
        var lookbackHours = configuration.GetValue("Anomalies:ConfirmationVelocity:LookbackHours", 24);
        var lookbackStart = now.AddHours(-lookbackHours);
        var baselineStart = lookbackStart.AddDays(-baselineDays);
        var baselineBucketCount = Math.Max(1, (int)Math.Ceiling(baselineDays * 24.0 * 60 / windowMinutes));

        var confirmations = await context.BeerConfirmations
            .Where(c => c.ConfirmedAt >= baselineStart)
            .Select(c => new ConfirmationRow(c.ConfirmedAt, c.ConfirmedByUserId))
            .ToListAsync();

        // "Overall" (bartenderId = null) plus one pass per bartender who confirmed
        // recently — each compares its own recent buckets against its own baseline
        // average, so a busy bartender's normal volume doesn't drown out a quiet one's
        // spike.
        var bartenderIds = confirmations
            .Where(c => c.ConfirmedAt >= lookbackStart)
            .Select(c => c.ConfirmedByUserId)
            .Distinct()
            .Cast<string?>()
            .ToList();
        bartenderIds.Add(null);

        var results = new List<AnomalyResponse>();
        foreach (var bartenderId in bartenderIds)
        {
            var scoped = bartenderId == null
                ? confirmations
                : confirmations.Where(c => c.ConfirmedByUserId == bartenderId).ToList();

            var baselineCount = scoped.Count(c => c.ConfirmedAt < lookbackStart);
            var baselineAverage = (double)baselineCount / baselineBucketCount;
            var recent = scoped.Where(c => c.ConfirmedAt >= lookbackStart);

            foreach (var bucket in recent.GroupBy(c => BucketIndex(c.ConfirmedAt, lookbackStart, windowMinutes)))
            {
                var actual = bucket.Count();
                if (actual < minimumCount)
                {
                    continue;
                }
                if (actual < baselineAverage * multiplier)
                {
                    continue;
                }

                var actorEmail = bartenderId == null
                    ? null
                    : await context.Users.Where(u => u.Id == bartenderId).Select(u => u.Email).FirstOrDefaultAsync();
                var who = bartenderId == null ? "overall" : actorEmail ?? bartenderId;

                results.Add(new AnomalyResponse(
                    "ConfirmationVelocitySpike",
                    lookbackStart.AddMinutes(bucket.Key * windowMinutes),
                    $"{actual} confirmations ({who}) vs. a baseline of {baselineAverage:F1} per {windowMinutes}min window",
                    bartenderId,
                    actorEmail,
                    "/admin/confirmations"));
            }
        }
        return results;
    }

    public static async Task<List<AnomalyResponse>> DetectOffHoursActivityAsync(
        ApplicationDbContext context, IConfiguration configuration, DateTime now)
    {
        var openHour = configuration.GetValue("Anomalies:OffHours:OpenHour", 10);
        var closeHour = configuration.GetValue("Anomalies:OffHours:CloseHour", 2);
        var timeZoneId = configuration["Anomalies:OffHours:TimeZoneId"];
        var lookbackHours = configuration.GetValue("Anomalies:OffHours:LookbackHours", 24);
        var maxItems = configuration.GetValue("Anomalies:OffHours:MaxItems", 50);
        var windowStart = now.AddHours(-lookbackHours);
        var tz = string.IsNullOrWhiteSpace(timeZoneId) ? null : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        var confirmations = await context.BeerConfirmations
            .Where(c => c.ConfirmedAt >= windowStart)
            .OrderByDescending(c => c.ConfirmedAt)
            .ToListAsync();

        var results = new List<AnomalyResponse>();
        foreach (var c in confirmations)
        {
            var local = tz == null ? c.ConfirmedAt : TimeZoneInfo.ConvertTimeFromUtc(c.ConfirmedAt, tz);
            var hour = local.Hour;
            // Tavern hours can span midnight (e.g. open 10am, close 2am) — when
            // CloseHour <= OpenHour, "in hours" wraps around rather than being a
            // simple [OpenHour, CloseHour) range.
            var inHours = closeHour > openHour
                ? hour >= openHour && hour < closeHour
                : hour >= openHour || hour < closeHour;
            if (inHours)
            {
                continue;
            }

            var actorEmail = await context.Users
                .Where(u => u.Id == c.ConfirmedByUserId).Select(u => u.Email).FirstOrDefaultAsync();
            results.Add(new AnomalyResponse(
                "OffHoursActivity",
                c.ConfirmedAt,
                $"Confirmation recorded at {local:HH:mm}",
                c.ConfirmedByUserId,
                actorEmail,
                "/admin/confirmations"));
            if (results.Count >= maxItems)
            {
                break;
            }
        }
        return results;
    }

    // #81: one entry per beer with at least one recent unavailability report — "more
    // prominent than a single one" is expressed in the summary text's count (and, since
    // OccurredAt is the most-recently-reported time, a beer getting repeatedly flagged
    // naturally sorts near the top of the combined list) rather than a separate severity
    // tier, per the issue's own "no need for anything fancier."
    public static async Task<List<AnomalyResponse>> DetectUnavailabilityReportsAsync(
        ApplicationDbContext context, IConfiguration configuration, DateTime now)
    {
        var lookbackHours = configuration.GetValue("Anomalies:UnavailabilityReports:LookbackHours", 24);
        var windowStart = now.AddHours(-lookbackHours);

        var reports = await context.UnavailabilityReports
            .Where(r => r.CreatedAt >= windowStart)
            .Select(r => new { r.BeerId, r.CreatedAt })
            .ToListAsync();

        var results = new List<AnomalyResponse>();
        foreach (var group in reports.GroupBy(r => r.BeerId))
        {
            var beerName = await context.Beers
                .Where(b => b.Id == group.Key)
                .Select(b => b.Name)
                .FirstOrDefaultAsync();
            if (beerName == null)
            {
                continue;
            }

            var count = group.Count();
            results.Add(new AnomalyResponse(
                "UnavailabilityReport",
                group.Max(r => r.CreatedAt),
                $"'{beerName}' flagged unavailable by {count} customer{(count == 1 ? "" : "s")} in the last {lookbackHours}h",
                null,
                null,
                $"/beers/{group.Key}"));
        }
        return results;
    }

    private static int BucketIndex(DateTime timestamp, DateTime windowStart, int windowMinutes) =>
        (int)((timestamp - windowStart).TotalMinutes / windowMinutes);

    private sealed record ConfirmationRow(DateTime ConfirmedAt, string ConfirmedByUserId);
}

public record AnomalyResponse(
    string Type,
    DateTime OccurredAt,
    string Summary,
    string? ActorId,
    string? ActorEmail,
    string DeepLink);
