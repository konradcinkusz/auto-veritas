using System.ComponentModel.DataAnnotations;
using AutoVeritas.OffersService.Domain;
using AutoVeritas.OffersService.Models;

namespace AutoVeritas.OffersService.Contracts;

public record FinancingOfferResponse(
    Guid Id,
    string Slug,
    string Provider,
    FinancingType Type,
    decimal? TinPercent,
    decimal? TaePercent,
    RepaymentStructure RepaymentStructure,
    string TermDescription,
    string DownPaymentDescription,
    string FeesDescription,
    decimal? MonthlyInstallment60Eur,
    decimal? TotalInterest60Eur,
    string BestFor,
    Confidence RateConfidence,
    string? SourceName,
    string? SourceUrl,
    DateTimeOffset LastVerifiedAt,
    DateTimeOffset? OfferValidUntil,
    DateTimeOffset? SourcePublishedAt,
    int DaysSinceVerification,
    FreshnessStatus RateFreshness,
    bool IsExpired,
    DateTimeOffset UpdatedAt)
{
    public static FinancingOfferResponse From(FinancingOffer offer, FreshnessCalculator freshness) => new(
        offer.Id,
        offer.Slug,
        offer.Provider,
        offer.Type,
        offer.TinPercent,
        offer.TaePercent,
        offer.RepaymentStructure,
        offer.TermDescription,
        offer.DownPaymentDescription,
        offer.FeesDescription,
        offer.MonthlyInstallment60Eur,
        offer.TotalInterest60Eur,
        offer.BestFor,
        offer.RateConfidence,
        offer.SourceName,
        offer.SourceUrl,
        offer.LastVerifiedAt,
        offer.OfferValidUntil,
        offer.SourcePublishedAt,
        freshness.DaysSince(offer.LastVerifiedAt),
        freshness.ForRates(offer.LastVerifiedAt, offer.OfferValidUntil),
        freshness.IsExpired(offer.OfferValidUntil),
        offer.UpdatedAt);
}

public class FinancingOfferRequest
{
    /// <summary>Stable identity for agent upserts; derived from the provider when omitted.</summary>
    [StringLength(160)]
    public string? Slug { get; set; }

    [Required]
    [StringLength(200)]
    public string Provider { get; set; } = string.Empty;

    // Trust-critical enums are required-nullable: a plain enum property would
    // default an omitted repaymentStructure to Linear — presenting a balloon
    // offer as the safest kind is the exact lie this product exists to expose.
    [Required]
    public FinancingType? Type { get; set; }

    [Range(0, 100)]
    public decimal? TinPercent { get; set; }

    [Range(0, 100)]
    public decimal? TaePercent { get; set; }

    [Required]
    public RepaymentStructure? RepaymentStructure { get; set; }

    [Required]
    [StringLength(200)]
    public string TermDescription { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string DownPaymentDescription { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string FeesDescription { get; set; } = string.Empty;

    [Range(0, 100_000)]
    public decimal? MonthlyInstallment60Eur { get; set; }

    [Range(0, 1_000_000)]
    public decimal? TotalInterest60Eur { get; set; }

    [Required]
    [StringLength(500)]
    public string BestFor { get; set; } = string.Empty;

    [Required]
    public Confidence? RateConfidence { get; set; }

    [StringLength(200)]
    public string? SourceName { get; set; }

    [Url]
    [StringLength(500)]
    public string? SourceUrl { get; set; }

    [Required]
    public DateTimeOffset? LastVerifiedAt { get; set; }

    public DateTimeOffset? OfferValidUntil { get; set; }

    public DateTimeOffset? SourcePublishedAt { get; set; }

    public void Apply(FinancingOffer offer, DateTimeOffset now)
    {
        offer.Provider = Provider;
        offer.Type = Type!.Value;
        offer.TinPercent = TinPercent;
        offer.TaePercent = TaePercent;
        offer.RepaymentStructure = RepaymentStructure!.Value;
        offer.TermDescription = TermDescription;
        offer.DownPaymentDescription = DownPaymentDescription;
        offer.FeesDescription = FeesDescription;
        offer.MonthlyInstallment60Eur = MonthlyInstallment60Eur;
        offer.TotalInterest60Eur = TotalInterest60Eur;
        offer.BestFor = BestFor;
        offer.RateConfidence = RateConfidence!.Value;
        offer.SourceName = SourceName;
        offer.SourceUrl = SourceUrl;
        // Normalized to UTC at the write boundary — see CarOfferRequest.Apply.
        offer.LastVerifiedAt = LastVerifiedAt!.Value.ToUniversalTime();
        offer.OfferValidUntil = OfferValidUntil?.ToUniversalTime();
        offer.SourcePublishedAt = SourcePublishedAt?.ToUniversalTime();
        offer.UpdatedAt = now;
    }
}
