using System.Security.Claims;
using BeerApi.Controllers;
using BeerApi.Data;
using BeerApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BeerApi.Tests.Controllers;

public class MeControllerTests
{
    private const string CustomerId = "customer-1";

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static MeController CreateController(ApplicationDbContext context, string userId = CustomerId)
    {
        return new MeController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test")),
                },
            },
        };
    }

    [Fact]
    public async Task GetProgress_WithNoConfirmations_ReturnsZeroOfGoal()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetProgress();

        var progress = Assert.IsType<ProgressResponse>(result.Value);
        Assert.Equal(0, progress.ConfirmedCount);
        Assert.Equal(ConfirmationsController.MugGoal, progress.Goal);
        Assert.False(progress.MugEarned);
        Assert.Null(progress.MugEarnedAt);
        Assert.Empty(progress.Confirmations);
    }

    // #14: earned status comes from the stored award, not the live count — it must
    // survive catalog churn and confirmation corrections.
    [Fact]
    public async Task GetProgress_WithAward_ReportsEarnedDate_IndependentOfLiveCount()
    {
        using var context = CreateContext();
        var earnedAt = new DateTime(2026, 7, 15, 21, 0, 0, DateTimeKind.Utc);
        context.MugAwards.Add(new MugAward { CustomerId = CustomerId, TavernId = 1, EarnedAt = earnedAt });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.GetProgress();

        var progress = Assert.IsType<ProgressResponse>(result.Value);
        Assert.Equal(0, progress.ConfirmedCount);
        Assert.True(progress.MugEarned);
        Assert.Equal(earnedAt, progress.MugEarnedAt);
    }

    [Fact]
    public async Task GetProgress_ReturnsOwnConfirmations_NewestFirst()
    {
        using var context = CreateContext();
        var older = new Beer { Name = "Pale Ale", Brewery = "Sierra Nevada", Style = "American Pale Ale" };
        var newer = new Beer { Name = "Duvel", Brewery = "Duvel Moortgat", Style = "Belgian Strong Golden Ale" };
        context.Beers.AddRange(older, newer);
        await context.SaveChangesAsync();

        context.BeerConfirmations.AddRange(
            new BeerConfirmation { CustomerId = CustomerId, BeerId = older.Id, TavernId = 1, ConfirmedByUserId = "b1", ConfirmedAt = DateTime.UtcNow.AddDays(-2) },
            new BeerConfirmation { CustomerId = CustomerId, BeerId = newer.Id, TavernId = 1, ConfirmedByUserId = "b1", ConfirmedAt = DateTime.UtcNow.AddDays(-1) },
            new BeerConfirmation { CustomerId = "someone-else", BeerId = older.Id, TavernId = 1, ConfirmedByUserId = "b1" });
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetProgress();

        var progress = Assert.IsType<ProgressResponse>(result.Value);
        Assert.Equal(2, progress.ConfirmedCount);
        Assert.Equal(new[] { "Duvel", "Pale Ale" }, progress.Confirmations.Select(c => c.Name));
        Assert.Equal("Duvel Moortgat", progress.Confirmations[0].Brewery);
    }

    [Fact]
    // #14 changed the contract: earned status comes from the stored award, never the
    // count. An at-goal count with no award (the stamp happens in the confirmation
    // flow) reports not-earned — count alone grants nothing.
    public async Task GetProgress_AtGoalWithoutAward_ReportsNotEarned()
    {
        using var context = CreateContext();
        for (var i = 0; i < ConfirmationsController.MugGoal; i++)
        {
            var beer = new Beer { Name = $"Beer {i}", Brewery = "B", Style = "S" };
            context.Beers.Add(beer);
            await context.SaveChangesAsync();
            context.BeerConfirmations.Add(new BeerConfirmation
            {
                CustomerId = CustomerId,
                BeerId = beer.Id,
                TavernId = 1,
                ConfirmedByUserId = "b1",
            });
        }
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetProgress();

        var progress = Assert.IsType<ProgressResponse>(result.Value);
        Assert.Equal(ConfirmationsController.MugGoal, progress.ConfirmedCount);
        Assert.False(progress.MugEarned);
        Assert.Null(progress.MugEarnedAt);
    }
}
