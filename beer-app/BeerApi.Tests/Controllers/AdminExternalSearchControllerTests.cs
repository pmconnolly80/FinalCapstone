using BeerApi.Controllers;
using BeerApi.Data;
using BeerApi.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BeerApi.Tests.Controllers;

// #83: demand report aggregation. Role gating is HTTP-level behavior covered by
// AdminExternalSearchTests; this covers ComputeDemandAsync directly with a fixed `now`
// (same testability pattern as AdminAnomaliesControllerTests).
public class AdminExternalSearchControllerTests
{
    private static readonly DateTime Now = new(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ComputeDemandAsync_GroupsByNormalizedQuery_OrderedByFrequency()
    {
        using var context = CreateContext();
        context.ExternalSearchLogs.AddRange(
            new ExternalSearchLog { CustomerId = "c1", Query = "Weird Sour", MatchedTavernCatalog = false, CreatedAt = Now.AddDays(-1) },
            new ExternalSearchLog { CustomerId = "c2", Query = "weird sour", MatchedTavernCatalog = false, CreatedAt = Now.AddDays(-2) },
            new ExternalSearchLog { CustomerId = "c1", Query = "  Weird Sour  ", MatchedTavernCatalog = false, CreatedAt = Now.AddDays(-3) },
            new ExternalSearchLog { CustomerId = "c1", Query = "Obscure Lager", MatchedTavernCatalog = false, CreatedAt = Now.AddDays(-1) });
        await context.SaveChangesAsync();

        var result = await AdminExternalSearchController.ComputeDemandAsync(context, Now);

        Assert.Equal("weird sour", result[0].Query);
        Assert.Equal(3, result[0].Count);
        Assert.Equal("obscure lager", result[1].Query);
        Assert.Equal(1, result[1].Count);
    }

    [Fact]
    public async Task ComputeDemandAsync_ExcludesMatchedTavernCatalogRows()
    {
        using var context = CreateContext();
        context.ExternalSearchLogs.Add(new ExternalSearchLog
        {
            CustomerId = "c1", Query = "Duvel", MatchedTavernCatalog = true, CreatedAt = Now.AddDays(-1),
        });
        await context.SaveChangesAsync();

        var result = await AdminExternalSearchController.ComputeDemandAsync(context, Now);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ComputeDemandAsync_ExcludesRowsOutsideSinceDaysWindow()
    {
        using var context = CreateContext();
        context.ExternalSearchLogs.Add(new ExternalSearchLog
        {
            CustomerId = "c1", Query = "Old Search", MatchedTavernCatalog = false, CreatedAt = Now.AddDays(-31),
        });
        await context.SaveChangesAsync();

        var result = await AdminExternalSearchController.ComputeDemandAsync(context, Now, sinceDays: 30);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ComputeDemandAsync_ReportsLastSearchedAt()
    {
        using var context = CreateContext();
        var earlier = Now.AddDays(-5);
        var later = Now.AddDays(-1);
        context.ExternalSearchLogs.AddRange(
            new ExternalSearchLog { CustomerId = "c1", Query = "Obscure Lager", MatchedTavernCatalog = false, CreatedAt = earlier },
            new ExternalSearchLog { CustomerId = "c2", Query = "Obscure Lager", MatchedTavernCatalog = false, CreatedAt = later });
        await context.SaveChangesAsync();

        var result = await AdminExternalSearchController.ComputeDemandAsync(context, Now);

        Assert.Equal(later, Assert.Single(result).LastSearchedAt);
    }

    [Fact]
    public async Task ComputeDemandAsync_RespectsTopN()
    {
        using var context = CreateContext();
        for (var i = 0; i < 5; i++)
        {
            context.ExternalSearchLogs.Add(new ExternalSearchLog
            {
                CustomerId = "c1", Query = $"beer-{i}", MatchedTavernCatalog = false, CreatedAt = Now.AddDays(-1),
            });
        }
        await context.SaveChangesAsync();

        var result = await AdminExternalSearchController.ComputeDemandAsync(context, Now, topN: 2);

        Assert.Equal(2, result.Count);
    }
}
