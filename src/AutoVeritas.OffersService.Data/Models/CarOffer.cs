namespace AutoVeritas.OffersService.Models;

/// <summary>
/// One car offer on the Spanish market, as verified by the owner's agent. The three
/// dates are deliberately distinct: when the agent last checked the values at source,
/// how long the seller declares the offer valid, and when the source itself was
/// published. A price without those dates is incomplete information in this market.
/// </summary>
public class CarOffer
{
    public Guid Id { get; set; }

    /// <summary>Stable identity for seeding and agent upserts; never shown to users.</summary>
    public required string Slug { get; set; }

    public required string Name { get; set; }

    /// <summary>Body / powertrain description, e.g. "SUV / PHEV".</summary>
    public required string Variant { get; set; }

    public DgtLabel DgtLabel { get; set; }

    public int PowerCv { get; set; }

    public decimal? CashPriceEur { get; set; }

    /// <summary>Price when taking the manufacturer's financing (often far below cash).</summary>
    public decimal? FinancedPriceEur { get; set; }

    public int? ReliabilityScore { get; set; }

    public string? ReliabilityText { get; set; }

    public int? BootLiters { get; set; }

    public string? Notes { get; set; }

    public Confidence PriceConfidence { get; set; }

    public string? SourceName { get; set; }

    public string? SourceUrl { get; set; }

    public DateTimeOffset LastVerifiedAt { get; set; }

    public DateTimeOffset? OfferValidUntil { get; set; }

    public DateTimeOffset? SourcePublishedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
