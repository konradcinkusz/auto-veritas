namespace AutoVeritas.OffersService.Models;

/// <summary>
/// A point-in-time snapshot of a <see cref="CarOffer"/>'s value fields as they
/// stood immediately before a <c>PUT</c> changed them. Keyed by the offer's
/// immutable <see cref="CarOffer.Id"/>, never by <see cref="CarOffer.Slug"/> or
/// name — both can be edited, and history must survive that edit intact.
///
/// Re-verification (<c>POST .../verify</c>) never writes a row here: it is
/// deliberately the cheap, values-unchanged operation, and a full snapshot on
/// every re-check would drown real changes in noise.
/// </summary>
public class CarOfferHistory
{
    public Guid Id { get; set; }

    public Guid CarOfferId { get; set; }

    public required string Name { get; set; }

    public required string Variant { get; set; }

    public DgtLabel DgtLabel { get; set; }

    public int PowerCv { get; set; }

    public decimal? CashPriceEur { get; set; }

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

    /// <summary>When this snapshot was captured — i.e. the moment it stopped being current.</summary>
    public DateTimeOffset RecordedAt { get; set; }

    /// <summary>The agent account that made the change, if the token carried an email claim.</summary>
    public string? ChangedByEmail { get; set; }
}
