using AutoVeritas.OffersService.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoVeritas.OffersService.Seeding;

public static class OffersSeeder
{
    /// <summary>
    /// Insert-if-missing by slug, never update: the seed file describes a fresh
    /// environment, and runtime edits made by the agent must survive every restart.
    /// </summary>
    public static async Task SeedAsync(OffersDbContext db, CancellationToken cancellationToken = default)
    {
        var existingCarSlugs = await db.CarOffers.Select(offer => offer.Slug).ToHashSetAsync(cancellationToken);
        foreach (var offer in SeedData.CarOffers().Where(offer => !existingCarSlugs.Contains(offer.Slug)))
        {
            db.CarOffers.Add(offer);
        }

        var existingFinancingSlugs = await db.FinancingOffers.Select(offer => offer.Slug).ToHashSetAsync(cancellationToken);
        foreach (var offer in SeedData.FinancingOffers().Where(offer => !existingFinancingSlugs.Contains(offer.Slug)))
        {
            db.FinancingOffers.Add(offer);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
