using BeerApi.Controllers;
using BeerApi.Data;
using BeerApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BeerApi.Tests.Controllers;

// Role gating ([Authorize(Roles = "Admin")]) is middleware behavior covered by the
// HTTP-level MugAwardsTests; this covers the list itself.
public class MugAwardsControllerTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetMugAwards_ListsEarnersWithEmails_LongestWaitingFirst()
    {
        using var context = CreateContext();
        context.Users.AddRange(
            new ApplicationUser { Id = "cust-early", Email = "early@example.com", UserName = "early@example.com" },
            new ApplicationUser { Id = "cust-late", Email = "late@example.com", UserName = "late@example.com" });
        context.MugAwards.AddRange(
            new MugAward { CustomerId = "cust-late", TavernId = 1, EarnedAt = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc) },
            new MugAward { CustomerId = "cust-early", TavernId = 1, EarnedAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc) });
        await context.SaveChangesAsync();
        var controller = new MugAwardsController(context);

        var result = await controller.GetMugAwards();

        var awards = Assert.IsAssignableFrom<IReadOnlyList<MugAwardResponse>>(result.Value);
        Assert.Equal(2, awards.Count);
        // Oldest earner first — they've been waiting for their physical mug the longest.
        Assert.Equal("early@example.com", awards[0].Email);
        Assert.Equal("late@example.com", awards[1].Email);
        Assert.Equal(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), awards[0].EarnedAt);
    }

    [Fact]
    public async Task GetMugAwards_WithNoEarners_ReturnsEmptyList()
    {
        using var context = CreateContext();
        var controller = new MugAwardsController(context);

        var result = await controller.GetMugAwards();

        var awards = Assert.IsAssignableFrom<IReadOnlyList<MugAwardResponse>>(result.Value);
        Assert.Empty(awards);
    }
}
