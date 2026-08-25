namespace AutoVeritas.OffersService.Models;

/// <summary>
/// A point-in-time snapshot of a <see cref="FinancingOffer"/>'s value fields as
/// they stood immediately before a <c>PUT</c> changed them. See
/// <see cref="CarOfferHistory"/> for why this is keyed by the offer's immutable
/// id rather than its slug, and why <c>verify</c> never writes a row here.
/// </summary>
public class FinancingOfferHistory
{
    public Guid Id { get; set; }

    public Guid FinancingOfferId { get; set; }

    public required string Provider { get; set; }

    public FinancingType Type { get; set; }

    public decimal? TinPercent { get; set; }

    public decimal? TaePercent { get; set; }

    public RepaymentStructure RepaymentStructure { get; set; }

    public required string TermDescription { get; set; }

    public required string DownPaymentDescription { get; set; }

    public required string FeesDescription { get; set; }

    public decimal? MonthlyInstallment60Eur { get; set; }

    public decimal? TotalInterest60Eur { get; set; }

    public required string BestFor { get; set; }

    public Confidence RateConfidence { get; set; }

    public string? SourceName { get; set; }

    public string? SourceUrl { get; set; }

    public DateTimeOffset LastVerifiedAt { get; set; }

    public DateTimeOffset? OfferValidUntil { get; set; }

    public DateTimeOffset? SourcePublishedAt { get; set; }

    public DateTimeOffset RecordedAt { get; set; }

    public string? ChangedByEmail { get; set; }
}
