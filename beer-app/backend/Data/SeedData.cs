using BeerApi.Models;
using Microsoft.AspNetCore.Identity;

namespace BeerApi.Data;

public static class SeedData
{
    private static readonly string[] Roles = { "Admin", "Bartender", "Customer" };

    private static readonly Beer[] SampleBeers =
    {
        new() { Name = "Pale Ale", Brewery = "Sierra Nevada", Style = "American Pale Ale", Description = "Piney, citrusy hop character with a caramel malt backbone." },
        new() { Name = "60 Minute IPA", Brewery = "Dogfish Head", Style = "IPA", Description = "Continuously hopped for a bold, bitter finish." },
        new() { Name = "Guinness Draught", Brewery = "Guinness", Style = "Irish Dry Stout", Description = "Roasted, creamy, and famously smooth on nitro." },
        new() { Name = "Hefeweizen", Brewery = "Weihenstephaner", Style = "German Wheat Beer", Description = "Banana and clove notes from classic weizen yeast." },
        new() { Name = "Fat Tire", Brewery = "New Belgium", Style = "Amber Ale", Description = "Toasty malt sweetness balanced by a light hop finish." },
        new() { Name = "Pilsner Urquell", Brewery = "Plzeňský Prazdroj", Style = "Czech Pilsner", Description = "The original pilsner — crisp, golden, and floral." },
        new() { Name = "Duvel", Brewery = "Duvel Moortgat", Style = "Belgian Strong Golden Ale", Description = "Deceptively light-bodied with a dry, spicy finish." },
        new() { Name = "Oatmeal Stout", Brewery = "Samuel Smith", Style = "Oatmeal Stout", Description = "Silky mouthfeel with notes of coffee and dark chocolate." },
    };

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var db = services.GetRequiredService<ApplicationDbContext>();
        if (!db.Beers.Any())
        {
            db.Beers.AddRange(SampleBeers);
            await db.SaveChangesAsync();
        }
    }
}
