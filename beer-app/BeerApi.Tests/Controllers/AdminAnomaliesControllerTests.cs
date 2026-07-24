using BeerApi.Controllers;
using BeerApi.Data;
using BeerApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BeerApi.Tests.Controllers;

// #58: each Detect*Async method takes an explicit `now` instead of reading
// DateTime.UtcNow internally, so these tests are deterministic regardless of when they
// actually run — bucket-boundary/off-hours logic reading the real clock would
// otherwise be flaky.
public class AdminAnomaliesControllerTests
{
    private static readonly DateTime Now = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?>? overrides = null) =>
        new ConfigurationBuilder().AddInMemoryCollection(overrides ?? new Dictionary<string, string?>()).Build();

    // ---- Bulk beer add ----

    [Fact]
    public async Task DetectBulkBeerAddAsync_FlagsBurstMeetingThreshold_AttributedToSingleAdmin()
    {
        using var context = CreateContext();
        context.Users.Add(new ApplicationUser { Id = "admin-1", Email = "admin1@example.com", UserName = "admin1@example.com" });
        for (var i = 0; i < 10; i++)
        {
            var beer = new Beer { Name = $"Burst Beer {i}", Brewery = "Burst Brewery", Style = "IPA", CreatedAt = Now.AddMinutes(-30) };
            context.Beers.Add(beer);
            await context.SaveChangesAsync();
            context.AdminAudits.Add(new AdminAudit
            {
                AdminUserId = "admin-1", EntityType = "Beer", EntityId = beer.Id.ToString(), Action = "Create",
                AfterSnapshot = beer.Name, Reason = string.Empty,
            });
        }
        await context.SaveChangesAsync();

        var results = await AdminAnomaliesController.DetectBulkBeerAddAsync(context, CreateConfiguration(), Now);

        var anomaly = Assert.Single(results);
        Assert.Equal("BulkBeerAdd", anomaly.Type);
        Assert.Equal("admin-1", anomaly.ActorId);
        Assert.Equal("admin1@example.com", anomaly.ActorEmail);
        Assert.Equal("/admin/beers", anomaly.DeepLink);
    }

    [Fact]
    public async Task DetectBulkBeerAddAsync_BelowThreshold_ReturnsNothing()
    {
        using var context = CreateContext();
        for (var i = 0; i < 9; i++)
        {
            context.Beers.Add(new Beer { Name = $"Beer {i}", Brewery = "Brewery", Style = "IPA", CreatedAt = Now.AddMinutes(-30) });
        }
        await context.SaveChangesAsync();

        var results = await AdminAnomaliesController.DetectBulkBeerAddAsync(context, CreateConfiguration(), Now);

        Assert.Empty(results);
    }

    [Fact]
    public async Task DetectBulkBeerAddAsync_MultipleAdmins_ReportsNullActor()
    {
        using var context = CreateContext();
        for (var i = 0; i < 10; i++)
        {
            var beer = new Beer { Name = $"Burst Beer {i}", Brewery = "Burst Brewery", Style = "IPA", CreatedAt = Now.AddMinutes(-30) };
            context.Beers.Add(beer);
            await context.SaveChangesAsync();
            context.AdminAudits.Add(new AdminAudit
            {
                AdminUserId = i < 5 ? "admin-1" : "admin-2", EntityType = "Beer", EntityId = beer.Id.ToString(), Action = "Create",
                AfterSnapshot = beer.Name, Reason = string.Empty,
            });
        }
        await context.SaveChangesAsync();

        var results = await AdminAnomaliesController.DetectBulkBeerAddAsync(context, CreateConfiguration(), Now);

        var anomaly = Assert.Single(results);
        Assert.Null(anomaly.ActorId);
    }

    // ---- Confirmation velocity ----

    private static void SeedConfirmations(ApplicationDbContext context, string bartenderId, DateTime timestamp, int count)
    {
        for (var i = 0; i < count; i++)
        {
            context.BeerConfirmations.Add(new BeerConfirmation
            {
                CustomerId = $"cust-{Guid.NewGuid()}", BeerId = 1, TavernId = 1,
                ConfirmedByUserId = bartenderId, ConfirmedAt = timestamp,
            });
        }
    }

    [Fact]
    public async Task DetectConfirmationVelocityAsync_SpikeAboveBaselineAndMinimum_IsFlagged()
    {
        using var context = CreateContext();
        context.Users.Add(new ApplicationUser { Id = "bartender-1", Email = "bartender1@example.com", UserName = "bartender1@example.com" });
        // Baseline: 7 confirmations spread across the 7-day baseline window (~0.04/bucket).
        SeedConfirmations(context, "bartender-1", Now.AddDays(-3), 7);
        // Recent: 5 confirmations in one bucket — clears both MinimumCount and the multiplier.
        SeedConfirmations(context, "bartender-1", Now.AddMinutes(-30), 5);
        await context.SaveChangesAsync();

        var results = await AdminAnomaliesController.DetectConfirmationVelocityAsync(context, CreateConfiguration(), Now);

        var anomaly = Assert.Single(results, a => a.ActorId == "bartender-1");
        Assert.Equal("ConfirmationVelocitySpike", anomaly.Type);
        Assert.Equal("bartender1@example.com", anomaly.ActorEmail);
        Assert.Equal("/admin/confirmations", anomaly.DeepLink);
    }

    [Fact]
    public async Task DetectConfirmationVelocityAsync_BelowMinimumCount_IsNotFlagged_EvenWithZeroBaseline()
    {
        using var context = CreateContext();
        // No baseline confirmations at all for this bartender, but only 4 recent — below
        // MinimumCount(5), so the noise floor blocks it despite baselineAverage being 0.
        SeedConfirmations(context, "bartender-2", Now.AddMinutes(-30), 4);
        await context.SaveChangesAsync();

        var results = await AdminAnomaliesController.DetectConfirmationVelocityAsync(context, CreateConfiguration(), Now);

        Assert.DoesNotContain(results, a => a.ActorId == "bartender-2");
    }

    [Fact]
    public async Task DetectConfirmationVelocityAsync_WithinNormalBaselineVariation_IsNotFlagged()
    {
        using var context = CreateContext();
        // Baseline: 600 confirmations over 168 hourly buckets (~3.57/bucket average).
        // Recent: 8 in one bucket — above MinimumCount but below baselineAverage * 3.0 (~10.7).
        // Offset well clear of the lookbackStart boundary (-24h) so these land as baseline,
        // not recent.
        for (var day = 2; day <= 7; day++)
        {
            SeedConfirmations(context, "bartender-3", Now.AddDays(-day), 100);
        }
        SeedConfirmations(context, "bartender-3", Now.AddMinutes(-30), 8);
        await context.SaveChangesAsync();

        var results = await AdminAnomaliesController.DetectConfirmationVelocityAsync(context, CreateConfiguration(), Now);

        Assert.DoesNotContain(results, a => a.ActorId == "bartender-3");
    }

    [Fact]
    public async Task DetectConfirmationVelocityAsync_OverallSpike_FlaggedEvenWhenNoSingleBartenderSpikes()
    {
        using var context = CreateContext();
        // 3 bartenders each confirm 2 beers in the same recent bucket (6 total) — each
        // individually below MinimumCount(5), but the "overall" (bartenderId = null)
        // signal sees all 6 and flags.
        SeedConfirmations(context, "bartender-a", Now.AddMinutes(-30), 2);
        SeedConfirmations(context, "bartender-b", Now.AddMinutes(-30), 2);
        SeedConfirmations(context, "bartender-c", Now.AddMinutes(-30), 2);
        await context.SaveChangesAsync();

        var results = await AdminAnomaliesController.DetectConfirmationVelocityAsync(context, CreateConfiguration(), Now);

        Assert.Contains(results, a => a.ActorId == null);
        Assert.DoesNotContain(results, a => a.ActorId == "bartender-a");
    }

    // ---- Off-hours activity ----

    [Fact]
    public async Task DetectOffHoursActivityAsync_ConfirmationAt3Am_IsFlagged()
    {
        using var context = CreateContext();
        context.Users.Add(new ApplicationUser { Id = "bartender-1", Email = "bartender1@example.com", UserName = "bartender1@example.com" });
        context.BeerConfirmations.Add(new BeerConfirmation
        {
            CustomerId = "cust-1", BeerId = 1, TavernId = 1,
            ConfirmedByUserId = "bartender-1", ConfirmedAt = new DateTime(2026, 7, 20, 3, 0, 0, DateTimeKind.Utc),
        });
        await context.SaveChangesAsync();

        var results = await AdminAnomaliesController.DetectOffHoursActivityAsync(context, CreateConfiguration(), Now);

        var anomaly = Assert.Single(results);
        Assert.Equal("OffHoursActivity", anomaly.Type);
        Assert.Equal("bartender1@example.com", anomaly.ActorEmail);
    }

    [Fact]
    public async Task DetectOffHoursActivityAsync_ConfirmationAt2Pm_IsNotFlagged()
    {
        using var context = CreateContext();
        context.BeerConfirmations.Add(new BeerConfirmation
        {
            CustomerId = "cust-1", BeerId = 1, TavernId = 1,
            ConfirmedByUserId = "bartender-1", ConfirmedAt = new DateTime(2026, 7, 19, 14, 0, 0, DateTimeKind.Utc),
        });
        await context.SaveChangesAsync();

        var results = await AdminAnomaliesController.DetectOffHoursActivityAsync(context, CreateConfiguration(), Now);

        Assert.Empty(results);
    }

    [Fact]
    public async Task DetectOffHoursActivityAsync_ConfirmationAt11Pm_WraparoundIsInHours_NotFlagged()
    {
        using var context = CreateContext();
        context.BeerConfirmations.Add(new BeerConfirmation
        {
            CustomerId = "cust-1", BeerId = 1, TavernId = 1,
            ConfirmedByUserId = "bartender-1", ConfirmedAt = new DateTime(2026, 7, 19, 23, 0, 0, DateTimeKind.Utc),
        });
        await context.SaveChangesAsync();

        var results = await AdminAnomaliesController.DetectOffHoursActivityAsync(context, CreateConfiguration(), Now);

        Assert.Empty(results);
    }

    // ---- #81 unavailability reports ----

    [Fact]
    public async Task DetectUnavailabilityReportsAsync_MultipleCustomers_OneEntryWithCountInSummary()
    {
        using var context = CreateContext();
        var beer = new Beer { Name = "Duvel", Brewery = "Duvel Moortgat", Style = "Belgian Strong Golden Ale" };
        context.Beers.Add(beer);
        await context.SaveChangesAsync();
        context.UnavailabilityReports.AddRange(
            new UnavailabilityReport { CustomerId = "cust-1", BeerId = beer.Id, CreatedAt = Now.AddHours(-2) },
            new UnavailabilityReport { CustomerId = "cust-2", BeerId = beer.Id, CreatedAt = Now.AddHours(-1) },
            new UnavailabilityReport { CustomerId = "cust-3", BeerId = beer.Id, CreatedAt = Now.AddMinutes(-10) });
        await context.SaveChangesAsync();

        var results = await AdminAnomaliesController.DetectUnavailabilityReportsAsync(context, CreateConfiguration(), Now);

        var anomaly = Assert.Single(results);
        Assert.Equal("UnavailabilityReport", anomaly.Type);
        Assert.Contains("Duvel", anomaly.Summary);
        Assert.Contains("3 customers", anomaly.Summary);
        Assert.Equal(Now.AddMinutes(-10), anomaly.OccurredAt);
        Assert.Equal($"/beers/{beer.Id}", anomaly.DeepLink);
        Assert.Null(anomaly.ActorId);
    }

    [Fact]
    public async Task DetectUnavailabilityReportsAsync_SingleReport_SingularWording()
    {
        using var context = CreateContext();
        var beer = new Beer { Name = "Duvel", Brewery = "Duvel Moortgat", Style = "Belgian Strong Golden Ale" };
        context.Beers.Add(beer);
        await context.SaveChangesAsync();
        context.UnavailabilityReports.Add(new UnavailabilityReport { CustomerId = "cust-1", BeerId = beer.Id, CreatedAt = Now.AddHours(-1) });
        await context.SaveChangesAsync();

        var results = await AdminAnomaliesController.DetectUnavailabilityReportsAsync(context, CreateConfiguration(), Now);

        var anomaly = Assert.Single(results);
        Assert.Contains("1 customer ", anomaly.Summary);
    }

    [Fact]
    public async Task DetectUnavailabilityReportsAsync_OutsideLookbackWindow_ReturnsNothing()
    {
        using var context = CreateContext();
        var beer = new Beer { Name = "Duvel", Brewery = "Duvel Moortgat", Style = "Belgian Strong Golden Ale" };
        context.Beers.Add(beer);
        await context.SaveChangesAsync();
        context.UnavailabilityReports.Add(new UnavailabilityReport { CustomerId = "cust-1", BeerId = beer.Id, CreatedAt = Now.AddHours(-25) });
        await context.SaveChangesAsync();

        var results = await AdminAnomaliesController.DetectUnavailabilityReportsAsync(context, CreateConfiguration(), Now);

        Assert.Empty(results);
    }

    [Fact]
    public async Task DetectUnavailabilityReportsAsync_DifferentBeers_SeparateEntries()
    {
        using var context = CreateContext();
        var duvel = new Beer { Name = "Duvel", Brewery = "Duvel Moortgat", Style = "Belgian Strong Golden Ale" };
        var orval = new Beer { Name = "Orval", Brewery = "Brasserie d'Orval", Style = "Belgian Pale Ale" };
        context.Beers.AddRange(duvel, orval);
        await context.SaveChangesAsync();
        context.UnavailabilityReports.AddRange(
            new UnavailabilityReport { CustomerId = "cust-1", BeerId = duvel.Id, CreatedAt = Now.AddHours(-1) },
            new UnavailabilityReport { CustomerId = "cust-1", BeerId = orval.Id, CreatedAt = Now.AddHours(-1) });
        await context.SaveChangesAsync();

        var results = await AdminAnomaliesController.DetectUnavailabilityReportsAsync(context, CreateConfiguration(), Now);

        Assert.Equal(2, results.Count);
    }
}
