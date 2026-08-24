using AutoVeritas.OffersService.Contracts;
using AutoVeritas.OffersService.Data;
using AutoVeritas.OffersService.Domain;
using AutoVeritas.OffersService.Models;
using AutoVeritas.OffersService.Seeding;
using AutoVeritas.ServiceDefaults;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoVeritas.OffersService.Endpoints;

public static class CarOfferEndpoints
{
    public static RouteGroupBuilder MapCarOfferReadEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/car-offers", ListAsync).WithName(EndpointNames.ListCarOffers);
        group.MapGet("/car-offers/{id:guid}", GetAsync).WithName(EndpointNames.GetCarOffer);
        return group;
    }

    public static RouteGroupBuilder MapCarOfferWriteEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/car-offers", CreateAsync)
            .AddEndpointFilter<ValidationFilter<CarOfferRequest>>()
            .WithName(EndpointNames.CreateCarOffer);
        group.MapPut("/car-offers/{id:guid}", UpdateAsync)
            .AddEndpointFilter<ValidationFilter<CarOfferRequest>>()
            .WithName(EndpointNames.UpdateCarOffer);
        group.MapDelete("/car-offers/{id:guid}", DeleteAsync).WithName(EndpointNames.DeleteCarOffer);
        group.MapPost("/car-offers/{id:guid}/verify", VerifyAsync)
            .AddEndpointFilter<ValidationFilter<VerifyRequest>>()
            .WithName(EndpointNames.VerifyCarOffer);
        return group;
    }

    private static async Task<Ok<ListResponse<CarOfferResponse>>> ListAsync(
        OffersDbContext db,
        FreshnessCalculator freshness,
        TimeProvider timeProvider,
        [FromQuery] string? search,
        [FromQuery] DgtLabel? label,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int? maxVerifiedAgeDays,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        // Clamping is a DoS control, not a nicety; the page ceiling also keeps
        // Skip's multiplication inside int range.
        page = Math.Clamp(page, 1, 100_000);
        limit = Math.Clamp(limit, 1, 100);

        var query = db.CarOffers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(offer =>
                offer.Name.ToLower().Contains(term)
                || offer.Variant.ToLower().Contains(term)
                || (offer.Notes != null && offer.Notes.ToLower().Contains(term)));
        }

        if (label is { } dgtLabel)
        {
            query = query.Where(offer => offer.DgtLabel == dgtLabel);
        }

        if (maxPrice is { } price)
        {
            // An offer fits the cap when EITHER known price fits; offers with no
            // price at all drop out of an explicitly price-capped view.
            query = query.Where(offer => offer.CashPriceEur <= price || offer.FinancedPriceEur <= price);
        }

        if (maxVerifiedAgeDays is { } maxAge)
        {
            var cutoff = timeProvider.GetUtcNow().AddDays(-Math.Max(0, maxAge));
            query = query.Where(offer => offer.LastVerifiedAt >= cutoff);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var offers = await query
            // Id as the final key makes the order total, so pages never
            // duplicate or drop rows that tie on the display keys.
            .OrderBy(offer => offer.Name).ThenBy(offer => offer.Variant).ThenBy(offer => offer.Id)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var items = offers.Select(offer => CarOfferResponse.From(offer, freshness)).ToList();
        return TypedResults.Ok(new ListResponse<CarOfferResponse>(items, totalCount, page, limit));
    }

    private static async Task<Results<Ok<CarOfferResponse>, NotFound>> GetAsync(
        Guid id, OffersDbContext db, FreshnessCalculator freshness, CancellationToken cancellationToken)
    {
        var offer = await db.CarOffers.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        return offer is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(CarOfferResponse.From(offer, freshness));
    }

    private static async Task<Results<Created<CarOfferResponse>, Conflict<ProblemDetails>, ValidationProblem>> CreateAsync(
        CarOfferRequest request, OffersDbContext db, FreshnessCalculator freshness, TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (OfferWriteGuards.FutureVerification(request.LastVerifiedAt, timeProvider) is { } problem)
        {
            return problem;
        }

        var slug = request.Slug is { Length: > 0 } explicitSlug
            ? explicitSlug
            : SlugGenerator.From($"{request.Name} {request.Variant}");
        if (slug.Length == 0)
        {
            return OfferWriteGuards.EmptySlug();
        }

        if (await db.CarOffers.AnyAsync(o => o.Slug == slug, cancellationToken))
        {
            return OfferWriteGuards.DuplicateSlug(slug);
        }

        var now = timeProvider.GetUtcNow();
        var offer = new CarOffer { Slug = slug, Name = request.Name, Variant = request.Variant, CreatedAt = now };
        request.Apply(offer, now);

        db.CarOffers.Add(offer);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two concurrent creates can both pass the existence check; the
            // unique index is the arbiter and the loser gets the same 409.
            return OfferWriteGuards.DuplicateSlug(slug);
        }

        var response = CarOfferResponse.From(offer, freshness);
        return TypedResults.Created($"/api/v1/car-offers/{offer.Id}", response);
    }

    private static async Task<Results<Ok<CarOfferResponse>, NotFound, Conflict<ProblemDetails>, ValidationProblem>> UpdateAsync(
        Guid id, CarOfferRequest request, OffersDbContext db, FreshnessCalculator freshness, TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (OfferWriteGuards.FutureVerification(request.LastVerifiedAt, timeProvider) is { } problem)
        {
            return problem;
        }

        var offer = await db.CarOffers.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (offer is null)
        {
            return TypedResults.NotFound();
        }

        if (request.Slug is { Length: > 0 } newSlug && newSlug != offer.Slug)
        {
            if (await db.CarOffers.AnyAsync(o => o.Slug == newSlug && o.Id != id, cancellationToken))
            {
                return OfferWriteGuards.DuplicateSlug(newSlug);
            }
            offer.Slug = newSlug;
        }

        request.Apply(offer, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(CarOfferResponse.From(offer, freshness));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync(
        Guid id, OffersDbContext db, CancellationToken cancellationToken)
    {
        var offer = await db.CarOffers.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (offer is null)
        {
            return TypedResults.NotFound();
        }

        db.CarOffers.Remove(offer);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<CarOfferResponse>, NotFound, ValidationProblem>> VerifyAsync(
        Guid id, VerifyRequest request, OffersDbContext db, FreshnessCalculator freshness, TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (OfferWriteGuards.FutureVerification(request.VerifiedAt, timeProvider) is { } problem)
        {
            return problem;
        }

        var offer = await db.CarOffers.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (offer is null)
        {
            return TypedResults.NotFound();
        }

        var now = timeProvider.GetUtcNow();
        offer.LastVerifiedAt = request.VerifiedAt?.ToUniversalTime() ?? now;
        offer.SourceName = request.SourceName ?? offer.SourceName;
        offer.SourceUrl = request.SourceUrl ?? offer.SourceUrl;
        offer.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(CarOfferResponse.From(offer, freshness));
    }
}
