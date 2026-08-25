namespace AutoVeritas.OffersService.Models;

/// <summary>
/// One financing option (bank credit, green credit, fintech, manufacturer financing or
/// subscription). The repayment structure is a first-class attribute: an advertised
/// "low monthly" hiding an 18 000 € balloon is exactly what this product exists to expose.
/// </summary>
public class FinancingOffer
{
    public Guid Id { get; set; }

    /// <summary>Stable identity for seeding and agent upserts; never shown to users.</summary>
    public required string Slug { get; set; }

    public required string Provider { get; set; }

    public FinancingType Type { get; set; }

    /// <summary>Nominal interest rate; null for subscriptions, which have no credit component.</summary>
    public decimal? TinPercent { get; set; }

    /// <summary>Effective annual rate including fees; null for subscriptions.</summary>
    public decimal? TaePercent { get; set; }

    public RepaymentStructure RepaymentStructure { get; set; }

    public required string TermDescription { get; set; }

    public required string DownPaymentDescription { get; set; }

    public required string FeesDescription { get; set; }

    /// <summary>Example installment for a 26 000 € loan over 60 months.</summary>
    public decimal? MonthlyInstallment60Eur { get; set; }

    /// <summary>Total interest paid in the 60-month example.</summary>
    public decimal? TotalInterest60Eur { get; set; }

    public required string BestFor { get; set; }

    public Confidence RateConfidence { get; set; }

    public string? SourceName { get; set; }

    public string? SourceUrl { get; set; }

    public DateTimeOffset LastVerifiedAt { get; set; }

    public DateTimeOffset? OfferValidUntil { get; set; }

    public DateTimeOffset? SourcePublishedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
