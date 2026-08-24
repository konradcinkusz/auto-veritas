using AutoVeritas.OffersService.Contracts;
using AutoVeritas.OffersService.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AutoVeritas.OffersService.Endpoints;

public static class MetaEndpoints
{
    public static RouteGroupBuilder MapMetaEndpoints(this RouteGroupBuilder group)
    {
        // The frontend renders its freshness legend from this instead of mirroring the
        // thresholds; the service is the single authority on what "fresh" means.
        group.MapGet("/meta/freshness-policy", GetFreshnessPolicy).WithName(EndpointNames.GetFreshnessPolicy);
        return group;
    }

    private static Ok<FreshnessPolicyResponse> GetFreshnessPolicy() =>
        TypedResults.Ok(new FreshnessPolicyResponse(
            FreshnessPolicy.PriceFreshDays,
            FreshnessPolicy.PriceWarningDays,
            FreshnessPolicy.RateFreshDays,
            FreshnessPolicy.RateWarningDays,
            FreshnessPolicy.SpecFreshDays,
            FreshnessPolicy.SpecWarningDays));
}
