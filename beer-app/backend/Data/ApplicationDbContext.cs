using BeerApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BeerApi.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Beer> Beers => Set<Beer>();
    public DbSet<Tavern> Taverns => Set<Tavern>();
    public DbSet<BeerConfirmation> BeerConfirmations => Set<BeerConfirmation>();
    public DbSet<StaffPin> StaffPins => Set<StaffPin>();
    public DbSet<FailedConfirmationAttempt> FailedConfirmationAttempts => Set<FailedConfirmationAttempt>();
    public DbSet<MugAward> MugAwards => Set<MugAward>();
    public DbSet<ConfirmationAudit> ConfirmationAudits => Set<ConfirmationAudit>();
    public DbSet<AdminAudit> AdminAudits => Set<AdminAudit>();
    public DbSet<ExternalSearchLog> ExternalSearchLogs => Set<ExternalSearchLog>();
    public DbSet<BeerRecommendation> BeerRecommendations => Set<BeerRecommendation>();
    public DbSet<BeerRating> BeerRatings => Set<BeerRating>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Stored as text so the rotating-inventory state is legible directly in the DB.
        builder.Entity<Beer>()
            .Property(b => b.Availability)
            .HasConversion<string>();

        builder.Entity<Beer>()
            .Property(b => b.Class)
            .HasConversion<string>();

        builder.Entity<BeerRecommendation>()
            .Property(r => r.Status)
            .HasConversion<string>();

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

        // The per-customer lockout check is a count over a recent time window.
        builder.Entity<FailedConfirmationAttempt>()
            .HasIndex(a => new { a.CustomerId, a.AttemptedAt });

        // One mug per customer, ever — the exactly-once stamp is a hard invariant.
        builder.Entity<MugAward>()
            .HasIndex(a => a.CustomerId)
            .IsUnique();

        // One rating per customer per beer — resubmitting edits it in place (#74).
        builder.Entity<BeerRating>()
            .HasIndex(r => new { r.CustomerId, r.BeerId })
            .IsUnique();

        builder.Entity<BeerRating>()
            .HasOne(r => r.Beer)
            .WithMany()
            .HasForeignKey(r => r.BeerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
