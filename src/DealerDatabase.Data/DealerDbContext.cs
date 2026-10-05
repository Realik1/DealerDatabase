using DealerDatabase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DealerDatabase.Data;

public class DealerDbContext(DbContextOptions<DealerDbContext> options) : DbContext(options)
{
    public DbSet<Dealer> Dealers => Set<Dealer>();
    public DbSet<DealerSource> DealerSources => Set<DealerSource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Dealer entity
        modelBuilder.Entity<Dealer>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(d => d.LegalName)
                .HasMaxLength(300);

            entity.Property(d => d.Address)
                .HasMaxLength(500);

            entity.Property(d => d.AddressLine2)
                .HasMaxLength(300);

            entity.Property(d => d.Postcode)
                .HasMaxLength(20);

            entity.Property(d => d.PhoneNumber)
                .HasMaxLength(20);

            entity.Property(d => d.WebsiteDomain)
                .HasMaxLength(255);

            entity.Property(d => d.CompaniesHouseNumber)
                .HasMaxLength(50);

            entity.Property(d => d.FcaFirmRefNumber)
                .HasMaxLength(50);

            entity.Property(d => d.VatNumber)
                .HasMaxLength(50);

            entity.Property(d => d.IcoRegistrationNumber)
                .HasMaxLength(50);

            entity.Property(d => d.SafMemberStatus)
                .HasMaxLength(100);

            entity.Property(d => d.Notes)
                .HasMaxLength(1000);

            // Relationships
            entity.HasMany(d => d.Sources)
                .WithOne(s => s.Dealer)
                .HasForeignKey(s => s.DealerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indices for common queries
            entity.HasIndex(d => d.CompaniesHouseNumber);
            entity.HasIndex(d => d.FcaFirmRefNumber);
            entity.HasIndex(d => d.VatNumber);
            entity.HasIndex(d => d.Name);
        });

        // Configure DealerSource entity
        modelBuilder.Entity<DealerSource>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.SourceName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(s => s.SourceId)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(s => s.SourceName_Value)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(s => s.SourceAddress)
                .HasMaxLength(500);

            entity.Property(s => s.SourcePostcode)
                .HasMaxLength(20);

            entity.Property(s => s.SourcePhoneNumber)
                .HasMaxLength(20);

            entity.Property(s => s.MatchReason)
                .HasMaxLength(100);

            // Unique constraint on source combination
            entity.HasIndex(s => new { s.SourceName, s.SourceId })
                .IsUnique();

            // Index for lookups
            entity.HasIndex(s => s.DealerId);
        });
    }
}
