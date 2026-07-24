using BeerApi.Controllers;
using BeerApi.Data;
using BeerApi.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BeerApi.Tests.Controllers;

// #59: ComputeSummaryAsync takes an explicit `now` instead of reading DateTime.UtcNow
// internally, so these tests are deterministic regardless of when they actually run —
// same lesson as #58's AdminAnomaliesController tests.
public class AdminDashboardControllerTests
{
    private static readonly DateTime Now = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ComputeSummaryAsync_TotalBeers_CountsAllRegardlessOfAvailability()
    {
        using var context = CreateContext();
        context.Beers.Add(new Beer { Name = "On Tap Beer", Brewery = "Brewery", Style = "IPA", Availability = BeerAvailability.OnTap });
        context.Beers.Add(new Beer { Name = "Retired Beer", Brewery = "Brewery", Style = "Stout", Availability = BeerAvailability.Retired });
        await context.SaveChangesAsync();

        var summary = await AdminDashboardController.ComputeSummaryAsync(context, Now);

        Assert.Equal(2, summary.TotalBeers);
    }

    [Fact]
    public async Task ComputeSummaryAsync_ConfirmationsToday_IncludesTodayMidnight_ExcludesYesterday()
    {
        using var context = CreateContext();
        context.BeerConfirmations.Add(new BeerConfirmation
        {
            CustomerId = "cust-1", BeerId = 1, TavernId = 1, ConfirmedByUserId = "bartender-1",
            ConfirmedAt = Now.Date, // today's UTC midnight — included
        });
        context.BeerConfirmations.Add(new BeerConfirmation
        {
            CustomerId = "cust-2", BeerId = 1, TavernId = 1, ConfirmedByUserId = "bartender-1",
            ConfirmedAt = Now.Date.AddMinutes(-1), // 23:59 the prior day — excluded
        });
        await context.SaveChangesAsync();

        var summary = await AdminDashboardController.ComputeSummaryAsync(context, Now);

        Assert.Equal(1, summary.ConfirmationsToday);
    }

    [Fact]
    public async Task ComputeSummaryAsync_ActiveMembers_29DaysCounts_31DaysDoesNot_DistinctPerCustomer()
    {
        using var context = CreateContext();
        // Within the 30-day window, 3 confirmations from the same customer — counts once.
        for (var i = 0; i < 3; i++)
        {
            context.BeerConfirmations.Add(new BeerConfirmation
            {
                CustomerId = "cust-recent", BeerId = 1, TavernId = 1, ConfirmedByUserId = "bartender-1",
                ConfirmedAt = Now.AddDays(-29 - i * 0.01),
            });
        }
        // Outside the window entirely.
        context.BeerConfirmations.Add(new BeerConfirmation
        {
            CustomerId = "cust-lapsed", BeerId = 1, TavernId = 1, ConfirmedByUserId = "bartender-1",
            ConfirmedAt = Now.AddDays(-31),
        });
        await context.SaveChangesAsync();

        var summary = await AdminDashboardController.ComputeSummaryAsync(context, Now);

        Assert.Equal(1, summary.ActiveMembers);
    }

    [Fact]
    public async Task ComputeSummaryAsync_MugsAwarded_CountsSeededRows()
    {
        using var context = CreateContext();
        context.MugAwards.Add(new MugAward { CustomerId = "cust-1", TavernId = 1, EarnedAt = Now.AddDays(-10) });
        context.MugAwards.Add(new MugAward { CustomerId = "cust-2", TavernId = 1, EarnedAt = Now.AddDays(-5) });
        await context.SaveChangesAsync();

        var summary = await AdminDashboardController.ComputeSummaryAsync(context, Now);

        Assert.Equal(2, summary.MugsAwarded);
    }

    // ---- #78 most/least-confirmed beers ----

    [Fact]
    public async Task ComputeBeerConfirmationCountsAsync_RanksByCount_MostAndLeastConfirmed()
    {
        using var context = CreateContext();
        var popular = new Beer { Name = "Duvel", Brewery = "Duvel Moortgat", Style = "Belgian Strong Golden Ale" };
        var unpopular = new Beer { Name = "Mystery Stout", Brewery = "Test Brewery", Style = "Stout" };
        context.Beers.AddRange(popular, unpopular);
        await context.SaveChangesAsync();

        for (var i = 0; i < 5; i++)
        {
            context.BeerConfirmations.Add(new BeerConfirmation
            {
                CustomerId = $"cust-{i}", BeerId = popular.Id, TavernId = 1, ConfirmedByUserId = "b1",
            });
        }
        await context.SaveChangesAsync();

        var result = await AdminDashboardController.ComputeBeerConfirmationCountsAsync(context);

        Assert.Equal("Duvel", result.MostConfirmed[0].Name);
        Assert.Equal(5, result.MostConfirmed[0].ConfirmedCount);
        Assert.Equal("Mystery Stout", result.LeastConfirmed[0].Name);
        Assert.Equal(0, result.LeastConfirmed[0].ConfirmedCount);
    }

    [Fact]
    public async Task ComputeBeerConfirmationCountsAsync_BeerWithZeroConfirmations_StillAppearsInLeastConfirmed()
    {
        // A plain GroupBy over BeerConfirmations alone would silently omit a beer with
        // no confirmations at all — exactly the "stout nobody's ordered" case this
        // feature exists for (PERSONAS_AND_USAGE.md).
        using var context = CreateContext();
        context.Beers.Add(new Beer { Name = "Never Confirmed", Brewery = "Test Brewery", Style = "Lager" });
        await context.SaveChangesAsync();

        var result = await AdminDashboardController.ComputeBeerConfirmationCountsAsync(context);

        Assert.Contains(result.LeastConfirmed, b => b.Name == "Never Confirmed" && b.ConfirmedCount == 0);
    }

    [Fact]
    public async Task ComputeBeerConfirmationCountsAsync_LimitsToTopN()
    {
        using var context = CreateContext();
        for (var i = 0; i < AdminDashboardController.TopN + 3; i++)
        {
            context.Beers.Add(new Beer { Name = $"Beer {i}", Brewery = "Test Brewery", Style = "Lager" });
        }
        await context.SaveChangesAsync();

        var result = await AdminDashboardController.ComputeBeerConfirmationCountsAsync(context);

        Assert.Equal(AdminDashboardController.TopN, result.MostConfirmed.Count);
        Assert.Equal(AdminDashboardController.TopN, result.LeastConfirmed.Count);
    }
}
