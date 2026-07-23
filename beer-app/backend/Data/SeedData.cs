using BeerApi.Models;
using Microsoft.AspNetCore.Identity;

namespace BeerApi.Data;

public static class SeedData
{
    private static readonly string[] Roles = { "Admin", "Bartender", "Customer" };

    public const string TavernName = "The Tavern";

    // Dev-only bootstrap so the confirmation flow is usable out of the box: a bartender
    // account with a known PIN. Real staff onboarding (owner-issued PINs, forced change on
    // first use) is Sprint 2 / Deployment & Hardening scope.
    public const string DevBartenderEmail = "bartender@example.com";
    public const string DevBartenderPassword = "Bartender1!";
    public const string DevBartenderPin = "123456";

    // Dev/testing-only accounts. Rotate or remove before production — see
    // TECHNICAL_ARCHITECTURE_PLAN.md's Deployment & Hardening scope.
    public const string DevAdminEmail = "admin@tavern.local";
    public const string DevAdminPassword = "admin1234";

    public const string DevTestCustomerEmail = "user1@gmail.com";
    public const string DevTestCustomerPassword = "1234User1#!";

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

        if (!db.Taverns.Any())
        {
            db.Taverns.Add(new Tavern { Name = TavernName });
            await db.SaveChangesAsync();
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var bartender = await userManager.FindByEmailAsync(DevBartenderEmail);
        if (bartender == null)
        {
            bartender = new ApplicationUser { UserName = DevBartenderEmail, Email = DevBartenderEmail };
            await userManager.CreateAsync(bartender, DevBartenderPassword);
            await userManager.AddToRoleAsync(bartender, "Bartender");
        }

        if (!db.StaffPins.Any(p => p.UserId == bartender.Id))
        {
            var hasher = new PasswordHasher<ApplicationUser>();
            db.StaffPins.Add(new StaffPin
            {
                UserId = bartender.Id,
                PinHash = hasher.HashPassword(bartender, DevBartenderPin),
            });
            await db.SaveChangesAsync();
        }

        var admin = await userManager.FindByEmailAsync(DevAdminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser { UserName = DevAdminEmail, Email = DevAdminEmail };
            await userManager.CreateAsync(admin, DevAdminPassword);
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        var testCustomer = await userManager.FindByEmailAsync(DevTestCustomerEmail);
        if (testCustomer == null)
        {
            testCustomer = new ApplicationUser { UserName = DevTestCustomerEmail, Email = DevTestCustomerEmail };
            await userManager.CreateAsync(testCustomer, DevTestCustomerPassword);
            await userManager.AddToRoleAsync(testCustomer, "Customer");
        }
    }
}
