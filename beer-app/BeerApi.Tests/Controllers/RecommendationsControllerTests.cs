using System.Security.Claims;
using BeerApi.Controllers;
using BeerApi.Data;
using BeerApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BeerApi.Tests.Controllers;

// #73: customer beer recommendations. Admin-only gating on the triage controller is
// HTTP-level behavior covered by AdminRecommendationsTests.
public class RecommendationsControllerTests
{
    private const string CustomerId = "customer-1";

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static RecommendationsController CreateController(ApplicationDbContext context, string? userId = CustomerId)
    {
        var claims = userId == null
            ? new ClaimsIdentity()
            : new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test");

        return new RecommendationsController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(claims) },
            },
        };
    }

    [Fact]
    public async Task Submit_PlainTextOnly_Succeeds()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.Submit(new SubmitRecommendationRequest("Some Great IPA"));

        var recommendation = Assert.IsType<BeerRecommendation>(Assert.IsType<CreatedAtActionResult>(result.Result).Value);
        Assert.Equal("Some Great IPA", recommendation.BeerName);
        Assert.Null(recommendation.BreweryName);
        Assert.Null(recommendation.ExternalCatalogBeerId);
        Assert.Equal(CustomerId, recommendation.CustomerId);
        Assert.Equal(BeerRecommendationStatus.New, recommendation.Status);
        Assert.Single(context.BeerRecommendations);
    }

    [Fact]
    public async Task Submit_WithExternalCatalogBeerIdAndNote_PersistsThem()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        await controller.Submit(new SubmitRecommendationRequest(
            "Duvel", "Duvel Moortgat", "cb-1", "Saw this on the lookup search"));

        var recommendation = Assert.Single(context.BeerRecommendations);
        Assert.Equal("Duvel Moortgat", recommendation.BreweryName);
        Assert.Equal("cb-1", recommendation.ExternalCatalogBeerId);
        Assert.Equal("Saw this on the lookup search", recommendation.Note);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Submit_WithoutBeerName_ReturnsBadRequest(string? beerName)
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.Submit(new SubmitRecommendationRequest(beerName!));

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(context.BeerRecommendations);
    }

    [Fact]
    public async Task Submit_WithoutCustomerId_ReturnsUnauthorized()
    {
        using var context = CreateContext();
        var controller = CreateController(context, userId: null);

        var result = await controller.Submit(new SubmitRecommendationRequest("Duvel"));

        Assert.IsType<UnauthorizedResult>(result.Result);
        Assert.Empty(context.BeerRecommendations);
    }
}
