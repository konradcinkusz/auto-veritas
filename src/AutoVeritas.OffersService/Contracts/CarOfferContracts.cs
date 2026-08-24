using System.ComponentModel.DataAnnotations;
using AutoVeritas.OffersService.Domain;
using AutoVeritas.OffersService.Models;

namespace AutoVeritas.OffersService.Contracts;

public record CarOfferResponse(
    Guid Id,
    string Slug,
    string Name,
    string Variant,
    DgtLabel DgtLabel,
    int PowerCv,
    decimal? CashPriceEur,
    decimal? FinancedPriceEur,
    decimal? PriceGapEur,
    int? ReliabilityScore,
    string? ReliabilityText,
    int? BootLiters,
    string? Notes,
    Confidence PriceConfidence,
    string? SourceName,
    string? SourceUrl,
    DateTimeOffset LastVerifiedAt,
    DateTimeOffset? OfferValidUntil,
    DateTimeOffset? SourcePublishedAt,
    int DaysSinceVerification,
    FreshnessStatus PriceFreshness,
    FreshnessStatus SpecFreshness,
    bool IsExpired,
    DateTimeOffset UpdatedAt)
{
    public static CarOfferResponse From(CarOffer offer, FreshnessCalculator freshness) => new(
        offer.Id,
        offer.Slug,
        offer.Name,
        offer.Variant,
        offer.DgtLabel,
        offer.PowerCv,
        offer.CashPriceEur,
        offer.FinancedPriceEur,
        offer.CashPriceEur is { } cash && offer.FinancedPriceEur is { } financed ? cash - financed : null,
        offer.ReliabilityScore,
        offer.ReliabilityText,
        offer.BootLiters,
        offer.Notes,
        offer.PriceConfidence,
        offer.SourceName,
        offer.SourceUrl,
        offer.LastVerifiedAt,
        offer.OfferValidUntil,
        offer.SourcePublishedAt,
        freshness.DaysSince(offer.LastVerifiedAt),
        freshness.ForPrice(offer.LastVerifiedAt, offer.OfferValidUntil),
        freshness.ForSpec(offer.LastVerifiedAt),
        freshness.IsExpired(offer.OfferValidUntil),
        offer.UpdatedAt);
}

public class CarOfferRequest
{
    /// <summary>Stable identity for agent upserts; derived from the name when omitted.</summary>
    [StringLength(160)]
    public string? Slug { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Variant { get; set; } = string.Empty;

    public DgtLabel DgtLabel { get; set; }

    [Range(1, 2000)]
    public int PowerCv { get; set; }

    [Range(0, 10_000_000)]
    public decimal? CashPriceEur { get; set; }

    [Range(0, 10_000_000)]
    public decimal? FinancedPriceEur { get; set; }

    [Range(0, 100)]
    public int? ReliabilityScore { get; set; }

    [StringLength(60)]
    public string? ReliabilityText { get; set; }

    [Range(0, 5000)]
    public int? BootLiters { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public Confidence PriceConfidence { get; set; }

    [StringLength(200)]
    public string? SourceName { get; set; }

    [Url]
    [StringLength(500)]
    public string? SourceUrl { get; set; }

    [Required]
    public DateTimeOffset? LastVerifiedAt { get; set; }

    public DateTimeOffset? OfferValidUntil { get; set; }

    public DateTimeOffset? SourcePublishedAt { get; set; }

    public void Apply(CarOffer offer, DateTimeOffset now)
    {
        offer.Name = Name;
        offer.Variant = Variant;
        offer.DgtLabel = DgtLabel;
        offer.PowerCv = PowerCv;
        offer.CashPriceEur = CashPriceEur;
        offer.FinancedPriceEur = FinancedPriceEur;
        offer.ReliabilityScore = ReliabilityScore;
        offer.ReliabilityText = ReliabilityText;
        offer.BootLiters = BootLiters;
        offer.Notes = Notes;
        offer.PriceConfidence = PriceConfidence;
        offer.SourceName = SourceName;
        offer.SourceUrl = SourceUrl;
        offer.LastVerifiedAt = LastVerifiedAt!.Value;
        offer.OfferValidUntil = OfferValidUntil;
        offer.SourcePublishedAt = SourcePublishedAt;
        offer.UpdatedAt = now;
    }
}
