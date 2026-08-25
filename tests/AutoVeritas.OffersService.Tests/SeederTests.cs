using AutoVeritas.OffersService.Seeding;
using AutoVeritas.OffersService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AutoVeritas.OffersService.Tests;

public class SeederTests : IntegrationTestBase
{
    [Fact]
    public async Task Seeding_twice_creates_no_duplicates()
    {
        await Factory.WithDbAsync(async db =>
        {
            await OffersSeeder.SeedAsync(db);

            Assert.Equal(17, await db.CarOffers.CountAsync());
            Assert.Equal(22, await db.FinancingOffers.CountAsync());
        });
    }

    [Fact]
    public async Task An_agent_edit_survives_reseeding()
    {
        await Factory.WithDbAsync(async db =>
        {
            var offer = await db.CarOffers.SingleAsync(o => o.Slug == "byd-atto-2-dm-i-active");
            offer.CashPriceEur = 24990;
            await db.SaveChangesAsync();
        });

        await Factory.WithDbAsync(async db =>
        {
            await OffersSeeder.SeedAsync(db);

            var offer = await db.CarOffers.SingleAsync(o => o.Slug == "byd-atto-2-dm-i-active");
            Assert.Equal(24990, offer.CashPriceEur);
        });
    }

    [Fact]
    public void Every_seeded_slug_is_unique_and_normalized()
    {
        var carSlugs = SeedData.CarOffers().Select(offer => offer.Slug).ToList();
        var financingSlugs = SeedData.FinancingOffers().Select(offer => offer.Slug).ToList();

        Assert.Equal(carSlugs.Count, carSlugs.Distinct().Count());
        Assert.Equal(financingSlugs.Count, financingSlugs.Distinct().Count());
        Assert.All(carSlugs.Concat(financingSlugs), slug => Assert.Equal(SlugGenerator.From(slug), slug));
    }

    [Fact]
    public void The_slug_generator_normalizes_diacritics_and_symbols()
    {
        Assert.Equal("kredyt-zielony-bbva-coche-ecologico", SlugGenerator.From("Kredyt 'zielony' — BBVA Coche Ecológico"));
        Assert.Equal("mg-zs-hybrid", SlugGenerator.From("MG ZS Hybrid+"));
    }
}
