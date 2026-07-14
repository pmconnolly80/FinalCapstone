using BeerApi.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BeerApi.Tests.Data;

[Collection("WebApplicationFactory")]
public class SeedDataTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();

    public SeedDataTests()
    {
        _factory.CreateClient(); // forces host startup, which runs SeedData.InitializeAsync once
    }

    [Fact]
    public async Task InitializeAsync_CreatesExpectedRoles()
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        Assert.True(await roleManager.RoleExistsAsync("Admin"));
        Assert.True(await roleManager.RoleExistsAsync("Bartender"));
        Assert.True(await roleManager.RoleExistsAsync("Customer"));
    }

    [Fact]
    public async Task InitializeAsync_SeedsSampleBeers()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.True(await db.Beers.AnyAsync());
    }

    [Fact]
    public async Task InitializeAsync_CalledAgain_DoesNotDuplicateBeers()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var countAfterStartup = await db.Beers.CountAsync();

        await SeedData.InitializeAsync(scope.ServiceProvider);

        Assert.Equal(countAfterStartup, await db.Beers.CountAsync());
    }

    public void Dispose() => _factory.Dispose();
}
