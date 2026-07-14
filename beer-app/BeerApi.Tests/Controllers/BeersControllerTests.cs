using BeerApi.Controllers;
using BeerApi.Data;
using BeerApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BeerApi.Tests.Controllers;

public class BeersControllerTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetBeers_ReturnsSeededBeers_OrderedByName()
    {
        using var context = CreateContext();
        context.Beers.AddRange(
            new Beer { Name = "Zythos", Brewery = "Brewery Z", Style = "IPA" },
            new Beer { Name = "Ale", Brewery = "Brewery A", Style = "Pale Ale" });
        await context.SaveChangesAsync();
        var controller = new BeersController(context);

        var result = await controller.GetBeers();

        var beers = Assert.IsAssignableFrom<IEnumerable<Beer>>(result.Value).ToList();
        Assert.Equal(new[] { "Ale", "Zythos" }, beers.Select(b => b.Name));
    }

    [Fact]
    public async Task GetBeer_WithKnownId_ReturnsBeer()
    {
        using var context = CreateContext();
        var beer = new Beer { Name = "Hefeweizen", Brewery = "Weihenstephaner", Style = "Wheat" };
        context.Beers.Add(beer);
        await context.SaveChangesAsync();
        var controller = new BeersController(context);

        var result = await controller.GetBeer(beer.Id);

        Assert.Equal("Hefeweizen", result.Value?.Name);
    }

    [Fact]
    public async Task GetBeer_WithUnknownId_ReturnsNotFound()
    {
        using var context = CreateContext();
        var controller = new BeersController(context);

        var result = await controller.GetBeer(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task PostBeer_AddsBeer_AndReturnsCreatedAtAction()
    {
        using var context = CreateContext();
        var controller = new BeersController(context);
        var beer = new Beer { Name = "Duvel", Brewery = "Duvel Moortgat", Style = "Belgian Strong Golden Ale" };

        var result = await controller.PostBeer(beer);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(BeersController.GetBeer), created.ActionName);
        Assert.Single(context.Beers);
    }

    [Fact]
    public async Task PutBeer_WithMismatchedId_ReturnsBadRequest()
    {
        using var context = CreateContext();
        var controller = new BeersController(context);
        var beer = new Beer { Id = 1, Name = "Fat Tire", Brewery = "New Belgium", Style = "Amber Ale" };

        var result = await controller.PutBeer(2, beer);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task PutBeer_WithMatchingId_UpdatesAndReturnsNoContent()
    {
        using var context = CreateContext();
        var beer = new Beer { Name = "Fat Tire", Brewery = "New Belgium", Style = "Amber Ale" };
        context.Beers.Add(beer);
        await context.SaveChangesAsync();
        context.Entry(beer).State = EntityState.Detached;

        var controller = new BeersController(context);
        var updated = new Beer { Id = beer.Id, Name = "Fat Tire", Brewery = "New Belgium", Style = "Amber Ale (updated)" };

        var result = await controller.PutBeer(beer.Id, updated);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("Amber Ale (updated)", (await context.Beers.FindAsync(beer.Id))?.Style);
    }

    [Fact]
    public async Task DeleteBeer_WithKnownId_RemovesBeer_AndReturnsNoContent()
    {
        using var context = CreateContext();
        var beer = new Beer { Name = "Pilsner Urquell", Brewery = "Plzeňský Prazdroj", Style = "Czech Pilsner" };
        context.Beers.Add(beer);
        await context.SaveChangesAsync();
        var controller = new BeersController(context);

        var result = await controller.DeleteBeer(beer.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(context.Beers);
    }

    [Fact]
    public async Task DeleteBeer_WithUnknownId_ReturnsNotFound()
    {
        using var context = CreateContext();
        var controller = new BeersController(context);

        var result = await controller.DeleteBeer(999);

        Assert.IsType<NotFoundResult>(result);
    }
}
