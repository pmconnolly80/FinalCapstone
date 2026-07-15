using BeerApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Beer> Beers => Set<Beer>();
    public DbSet<Tavern> Taverns => Set<Tavern>();
    public DbSet<BeerConfirmation> BeerConfirmations => Set<BeerConfirmation>();
    public DbSet<StaffPin> StaffPins => Set<StaffPin>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // A beer counts once per customer, ever — the paper sheet has one initial per line.
        builder.Entity<BeerConfirmation>()
            .HasIndex(c => new { c.CustomerId, c.BeerId })
            .IsUnique();

        builder.Entity<BeerConfirmation>()
            .HasOne(c => c.Beer)
            .WithMany()
            .HasForeignKey(c => c.BeerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BeerConfirmation>()
            .HasOne(c => c.Tavern)
            .WithMany()
            .HasForeignKey(c => c.TavernId)
            .OnDelete(DeleteBehavior.Restrict);

        // The server resolves bartender identity from the PIN alone, so one PIN per staff
        // user is the invariant; uniqueness of the PIN value itself is enforced at set time
        // (hashes can't be indexed for equality of the underlying PIN).
        builder.Entity<StaffPin>()
            .HasIndex(p => p.UserId)
            .IsUnique();
    }
}
