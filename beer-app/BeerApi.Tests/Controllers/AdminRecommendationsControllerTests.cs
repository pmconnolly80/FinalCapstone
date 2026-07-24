using System.Linq;
using BeerApi.Controllers;
using BeerApi.Data;
using BeerApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BeerApi.Tests.Controllers;

// Admin-only role gating is HTTP-level behavior covered by AdminRecommendationsTests;
// these cover the filterable list and status transition logic.
public class AdminRecommendationsControllerTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task SeedAsync(ApplicationDbContext context)
    {
        context.Users.Add(new ApplicationUser { Id = "customer-1", Email = "customer@example.com", UserName = "customer@example.com" });
        context.BeerRecommendations.AddRange(
            new BeerRecommendation { CustomerId = "customer-1", BeerName = "Duvel", Status = BeerRecommendationStatus.New },
            new BeerRecommendation { CustomerId = "customer-1", BeerName = "Orval", Status = BeerRecommendationStatus.Added });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetRecommendations_NoFilter_ListsAllWithCustomerEmail()
    {
        using var context = CreateContext();
        await SeedAsync(context);
        var controller = new AdminRecommendationsController(context);

        var result = await controller.GetRecommendations(null);

        var rows = Assert.IsAssignableFrom<IReadOnlyList<AdminRecommendationResponse>>(result.Value);
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, r => r.BeerName == "Duvel" && r.CustomerEmail == "customer@example.com");
    }

    [Fact]
    public async Task GetRecommendations_FiltersByStatus()
    {
        using var context = CreateContext();
        await SeedAsync(context);
        var controller = new AdminRecommendationsController(context);

        var result = await controller.GetRecommendations("Added");

        var rows = Assert.IsAssignableFrom<IReadOnlyList<AdminRecommendationResponse>>(result.Value);
        Assert.Equal("Orval", Assert.Single(rows).BeerName);
    }

    [Fact]
    public async Task GetRecommendations_UnknownStatus_ReturnsBadRequest()
    {
        using var context = CreateContext();
        await SeedAsync(context);
        var controller = new AdminRecommendationsController(context);

        var result = await controller.GetRecommendations("NotAStatus");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateStatus_ChangesStatus_NoReasonRequired()
    {
        using var context = CreateContext();
        await SeedAsync(context);
        var recommendation = context.BeerRecommendations.First(r => r.BeerName == "Duvel");
        var controller = new AdminRecommendationsController(context);

        var result = await controller.UpdateStatus(recommendation.Id, new UpdateRecommendationStatusRequest(BeerRecommendationStatus.Reviewed));

        Assert.IsType<NoContentResult>(result);
        var updated = await context.BeerRecommendations.FindAsync(recommendation.Id);
        Assert.Equal(BeerRecommendationStatus.Reviewed, updated!.Status);
    }

    [Fact]
    public async Task UpdateStatus_UnknownId_ReturnsNotFound()
    {
        using var context = CreateContext();
        await SeedAsync(context);
        var controller = new AdminRecommendationsController(context);

        var result = await controller.UpdateStatus(999, new UpdateRecommendationStatusRequest(BeerRecommendationStatus.Declined));

        Assert.IsType<NotFoundResult>(result);
    }
}
