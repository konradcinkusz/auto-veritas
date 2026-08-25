using System.ComponentModel.DataAnnotations;

namespace AutoVeritas.OffersService.Contracts;

public record ListResponse<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int Limit);

/// <summary>
/// The agent re-checked an offer at source and the values held: only the verification
/// timestamp moves. Re-verification without change is the cheapest trust-building
/// operation the agent has, so it gets its own endpoint.
/// </summary>
public class VerifyRequest
{
    public DateTimeOffset? VerifiedAt { get; set; }

    [StringLength(200)]
    public string? SourceName { get; set; }

    [Url]
    [StringLength(500)]
    public string? SourceUrl { get; set; }
}

public record FreshnessPolicyResponse(
    int PriceFreshDays,
    int PriceWarningDays,
    int RateFreshDays,
    int RateWarningDays,
    int SpecFreshDays,
    int SpecWarningDays);
