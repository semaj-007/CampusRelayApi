using CampusRelay.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusRelay.Api.Data;

/// <summary>
/// EF Core Code-First context for the whole schema in Part 1's "Data Models and
/// Schema Definitions" section. SQLite in Development, PostgreSQL in Production on Render,
/// Azure SQL Database for other deployments - see Program.cs for the provider switch.
/// </summary>
public class CampusRelayDbContext : DbContext
{
    public CampusRelayDbContext(DbContextOptions<CampusRelayDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<DeliveryRequest> DeliveryRequests => Set<DeliveryRequest>();
    public DbSet<MarketplaceListing> MarketplaceListings => Set<MarketplaceListing>();
    public DbSet<RideOffer> RideOffers => Set<RideOffer>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<BlockedUser> BlockedUsers => Set<BlockedUser>();
    public DbSet<ModerationReport> ModerationReports => Set<ModerationReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.SsoSub).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<DeliveryRequest>(entity =>
        {
            entity.Property(d => d.ItemPhotoUrls)
                .HasConversion(JsonStringListConverter.Converter)
                .Metadata.SetValueComparer(JsonStringListConverter.Comparer);

            entity.HasOne(d => d.Requester)
                .WithMany()
                .HasForeignKey(d => d.RequesterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MarketplaceListing>(entity =>
        {
            entity.Property(m => m.PhotoUrls)
                .HasConversion(JsonStringListConverter.Converter)
                .Metadata.SetValueComparer(JsonStringListConverter.Comparer);

            entity.HasOne(m => m.Seller)
                .WithMany()
                .HasForeignKey(m => m.SellerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RideOffer>(entity =>
        {
            entity.HasOne(r => r.Driver)
                .WithMany()
                .HasForeignKey(r => r.DriverId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasOne(t => t.Requester)
                .WithMany()
                .HasForeignKey(t => t.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Provider)
                .WithMany()
                .HasForeignKey(t => t.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasOne(m => m.Transaction)
                .WithMany()
                .HasForeignKey(m => m.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasIndex(r => r.TransactionId).IsUnique();

            entity.HasOne(r => r.Transaction)
                .WithMany()
                .HasForeignKey(r => r.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Reviewer)
                .WithMany()
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Reviewee)
                .WithMany()
                .HasForeignKey(r => r.RevieweeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BlockedUser>(entity =>
        {
            entity.HasKey(b => new { b.BlockerId, b.BlockedId });

            entity.HasOne(b => b.Blocker)
                .WithMany()
                .HasForeignKey(b => b.BlockerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Blocked)
                .WithMany()
                .HasForeignKey(b => b.BlockedId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ModerationReport>(entity =>
        {
            entity.HasOne(m => m.Reporter)
                .WithMany()
                .HasForeignKey(m => m.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
