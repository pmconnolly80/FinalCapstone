namespace BeerList.Migrations
{
    
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using BeersList.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<BeersList.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(BeersList.Models.ApplicationDbContext context)
        {
            context.Beers.AddOrUpdate(p => p.Name,
                new Beer
                {
                    Name = "Caldera IPA",
                    Brewery = "Caldera Brewing Company",
                    Style = "IPA",
                },
                new Beer
                {
                    Name = "Sweetwater IPA",
                    Brewery = "Sweeetwater Brewing Company",
                    Style = "IPA",
                },
                new Beer
                {
                    Name = "Harpoon IPA",
                    Brewery = "Harpoon Brewing Company",
                    Style = "IPA",
                }
                );
        }
    }
}
