using AutoVeritas.OffersService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AutoVeritas.OffersService.Data;

public class OffersDbContext(DbContextOptions<OffersDbContext> options) : DbContext(options)
{
    public DbSet<CarOffer> CarOffers => Set<CarOffer>();

    public DbSet<FinancingOffer> FinancingOffers => Set<FinancingOffer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SQLite (the test provider) cannot compare DateTimeOffset columns; storing
        // UTC ticks keeps filters and ordering chronological regardless of the
        // offset a caller sent. PostgreSQL keeps its native timestamptz mapping.
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            var utcTicks = new ValueConverter<DateTimeOffset, long>(
                value => value.UtcTicks,
                value => new DateTimeOffset(value, TimeSpan.Zero));

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties()
                             .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?)))
                {
                    property.SetValueConverter(utcTicks);
                }
            }
        }

        modelBuilder.Entity<CarOffer>(entity =>
        {
            entity.HasIndex(offer => offer.Slug).IsUnique();
            entity.Property(offer => offer.Slug).HasMaxLength(160);
            entity.Property(offer => offer.Name).HasMaxLength(200);
            entity.Property(offer => offer.Variant).HasMaxLength(120);
            entity.Property(offer => offer.ReliabilityText).HasMaxLength(60);
            entity.Property(offer => offer.Notes).HasMaxLength(500);
            entity.Property(offer => offer.SourceName).HasMaxLength(200);
            entity.Property(offer => offer.SourceUrl).HasMaxLength(500);
            entity.Property(offer => offer.DgtLabel).HasConversion<string>().HasMaxLength(10);
            entity.Property(offer => offer.PriceConfidence).HasConversion<string>().HasMaxLength(20);
            entity.Property(offer => offer.CashPriceEur).HasPrecision(12, 2);
            entity.Property(offer => offer.FinancedPriceEur).HasPrecision(12, 2);
        });

        modelBuilder.Entity<FinancingOffer>(entity =>
        {
            entity.HasIndex(offer => offer.Slug).IsUnique();
            entity.Property(offer => offer.Slug).HasMaxLength(160);
            entity.Property(offer => offer.Provider).HasMaxLength(200);
            entity.Property(offer => offer.TermDescription).HasMaxLength(200);
            entity.Property(offer => offer.DownPaymentDescription).HasMaxLength(200);
            entity.Property(offer => offer.FeesDescription).HasMaxLength(200);
            entity.Property(offer => offer.BestFor).HasMaxLength(500);
            entity.Property(offer => offer.SourceName).HasMaxLength(200);
            entity.Property(offer => offer.SourceUrl).HasMaxLength(500);
            entity.Property(offer => offer.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(offer => offer.RepaymentStructure).HasConversion<string>().HasMaxLength(20);
            entity.Property(offer => offer.RateConfidence).HasConversion<string>().HasMaxLength(20);
            entity.Property(offer => offer.TinPercent).HasPrecision(6, 2);
            entity.Property(offer => offer.TaePercent).HasPrecision(6, 2);
            entity.Property(offer => offer.MonthlyInstallment60Eur).HasPrecision(12, 2);
            entity.Property(offer => offer.TotalInterest60Eur).HasPrecision(12, 2);
        });
    }
}
