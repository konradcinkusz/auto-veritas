using System.Security.Claims;
using AutoVeritas.OffersService.Contracts;
using AutoVeritas.OffersService.Data;
using AutoVeritas.OffersService.Domain;
using AutoVeritas.OffersService.Extensions;
using AutoVeritas.OffersService.Models;
using AutoVeritas.OffersService.Seeding;
using AutoVeritas.ServiceDefaults;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoVeritas.OffersService.Endpoints;

public static class FinancingOfferEndpoints
{
    private const int MaxHistoryEntries = 100;

    public static RouteGroupBuilder MapFinancingOfferReadEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/financing-offers", ListAsync).WithName(EndpointNames.ListFinancingOffers);
        group.MapGet("/financing-offers/{id:guid}", GetAsync).WithName(EndpointNames.GetFinancingOffer);
        group.MapGet("/financing-offers/{id:guid}/history", GetHistoryAsync).WithName(EndpointNames.GetFinancingOfferHistory);
        return group;
    }

    public static RouteGroupBuilder MapFinancingOfferWriteEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/financing-offers", CreateAsync)
            .AddEndpointFilter<ValidationFilter<FinancingOfferRequest>>()
            .WithName(EndpointNames.CreateFinancingOffer);
        group.MapPut("/financing-offers/{id:guid}", UpdateAsync)
            .AddEndpointFilter<ValidationFilter<FinancingOfferRequest>>()
            .WithName(EndpointNames.UpdateFinancingOffer);
        group.MapDelete("/financing-offers/{id:guid}", DeleteAsync).WithName(EndpointNames.DeleteFinancingOffer);
        group.MapPost("/financing-offers/{id:guid}/verify", VerifyAsync)
            .AddEndpointFilter<ValidationFilter<VerifyRequest>>()
            .WithName(EndpointNames.VerifyFinancingOffer);
        return group;
    }

    private static async Task<Ok<ListResponse<FinancingOfferResponse>>> ListAsync(
        OffersDbContext db,
        FreshnessCalculator freshness,
        TimeProvider timeProvider,
        [FromQuery] string? search,
        [FromQuery] FinancingType? type,
        [FromQuery] decimal? maxTin,
        [FromQuery] int? maxVerifiedAgeDays,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        // Clamping is a DoS control; the page ceiling keeps Skip inside int range.
        page = Math.Clamp(page, 1, 100_000);
        limit = Math.Clamp(limit, 1, 100);

        var query = db.FinancingOffers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(offer =>
                offer.Provider.ToLower().Contains(term)
                || offer.BestFor.ToLower().Contains(term));
        }

        if (type is { } financingType)
        {
            query = query.Where(offer => offer.Type == financingType);
        }

        if (maxTin is { } tin)
        {
            // Subscriptions have no TIN and always pass the rate filter.
            query = query.Where(offer => offer.TinPercent == null || offer.TinPercent <= tin);
        }

        if (maxVerifiedAgeDays is { } maxAge)
        {
            var cutoff = timeProvider.GetUtcNow().AddDays(-Math.Max(0, maxAge));
            query = query.Where(offer => offer.LastVerifiedAt >= cutoff);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var offers = await query
            // Id as the final key makes the order total across pages.
            .OrderBy(offer => offer.Provider).ThenBy(offer => offer.Id)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var items = offers.Select(offer => FinancingOfferResponse.From(offer, freshness)).ToList();
        return TypedResults.Ok(new ListResponse<FinancingOfferResponse>(items, totalCount, page, limit));
    }

    private static async Task<Results<Ok<FinancingOfferResponse>, NotFound>> GetAsync(
        Guid id, OffersDbContext db, FreshnessCalculator freshness, CancellationToken cancellationToken)
    {
        var offer = await db.FinancingOffers.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        return offer is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(FinancingOfferResponse.From(offer, freshness));
    }

    private static async Task<Results<Ok<IReadOnlyList<FinancingOfferHistoryEntryResponse>>, NotFound>> GetHistoryAsync(
        Guid id, OffersDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.FinancingOffers.AsNoTracking().AnyAsync(o => o.Id == id, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        var rows = await db.FinancingOfferHistories.AsNoTracking()
            .Where(row => row.FinancingOfferId == id)
            .OrderByDescending(row => row.RecordedAt)
            .Take(MaxHistoryEntries)
            .ToListAsync(cancellationToken);

        IReadOnlyList<FinancingOfferHistoryEntryResponse> response = rows.Select(FinancingOfferHistoryEntryResponse.From).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Created<FinancingOfferResponse>, Conflict<ProblemDetails>, ValidationProblem>> CreateAsync(
        FinancingOfferRequest request, OffersDbContext db, FreshnessCalculator freshness, TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (OfferWriteGuards.FutureVerification(request.LastVerifiedAt, timeProvider) is { } problem)
        {
            return problem;
        }

        var slug = request.Slug is { Length: > 0 } explicitSlug
            ? explicitSlug
            : SlugGenerator.From(request.Provider);
        if (slug.Length == 0)
        {
            return OfferWriteGuards.EmptySlug();
        }

        if (await db.FinancingOffers.AnyAsync(o => o.Slug == slug, cancellationToken))
        {
            return OfferWriteGuards.DuplicateSlug(slug);
        }

        var now = timeProvider.GetUtcNow();
        var offer = new FinancingOffer
        {
            Slug = slug,
            Provider = request.Provider,
            TermDescription = request.TermDescription,
            DownPaymentDescription = request.DownPaymentDescription,
            FeesDescription = request.FeesDescription,
            BestFor = request.BestFor,
            CreatedAt = now,
        };
        request.Apply(offer, now);

        db.FinancingOffers.Add(offer);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The unique index arbitrates concurrent duplicate creates.
            return OfferWriteGuards.DuplicateSlug(slug);
        }

        var response = FinancingOfferResponse.From(offer, freshness);
        return TypedResults.Created($"/api/v1/financing-offers/{offer.Id}", response);
    }

    private static async Task<Results<Ok<FinancingOfferResponse>, NotFound, Conflict<ProblemDetails>, ValidationProblem>> UpdateAsync(
        Guid id, FinancingOfferRequest request, OffersDbContext db, FreshnessCalculator freshness, TimeProvider timeProvider,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (OfferWriteGuards.FutureVerification(request.LastVerifiedAt, timeProvider) is { } problem)
        {
            return problem;
        }

        var offer = await db.FinancingOffers.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (offer is null)
        {
            return TypedResults.NotFound();
        }

        if (request.Slug is { Length: > 0 } newSlug && newSlug != offer.Slug)
        {
            if (await db.FinancingOffers.AnyAsync(o => o.Slug == newSlug && o.Id != id, cancellationToken))
            {
                return OfferWriteGuards.DuplicateSlug(newSlug);
            }
            offer.Slug = newSlug;
        }

        var before = FinancingOfferSnapshot.From(offer);
        var now = timeProvider.GetUtcNow();
        request.Apply(offer, now);

        if (before != FinancingOfferSnapshot.From(offer))
        {
            db.FinancingOfferHistories.Add(before.ToHistoryRow(offer.Id, now, user.GetEmail()));
        }

        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(FinancingOfferResponse.From(offer, freshness));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync(
        Guid id, OffersDbContext db, CancellationToken cancellationToken)
    {
        var offer = await db.FinancingOffers.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (offer is null)
        {
            return TypedResults.NotFound();
        }

        db.FinancingOffers.Remove(offer);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<FinancingOfferResponse>, NotFound, ValidationProblem>> VerifyAsync(
        Guid id, VerifyRequest request, OffersDbContext db, FreshnessCalculator freshness, TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (OfferWriteGuards.FutureVerification(request.VerifiedAt, timeProvider) is { } problem)
        {
            return problem;
        }

        var offer = await db.FinancingOffers.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
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

        return TypedResults.Ok(FinancingOfferResponse.From(offer, freshness));
    }
}
