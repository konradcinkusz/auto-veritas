using AutoVeritas.OffersService.Models;

namespace AutoVeritas.OffersService.Endpoints;

/// <summary>
/// A copy of an offer's mutable value fields, taken before <c>PUT</c> applies a
/// request to the tracked entity. Records get structural equality for free, so
/// comparing a before/after pair is the entire "did anything actually change"
/// check — no field-by-field diffing to keep in sync by hand.
/// </summary>
internal sealed record CarOfferSnapshot(
    string Name, string Variant, DgtLabel DgtLabel, int PowerCv,
    decimal? CashPriceEur, decimal? FinancedPriceEur, int? ReliabilityScore, string? ReliabilityText,
    int? BootLiters, string? Notes, Confidence PriceConfidence, string? SourceName, string? SourceUrl,
    DateTimeOffset LastVerifiedAt, DateTimeOffset? OfferValidUntil, DateTimeOffset? SourcePublishedAt)
{
    public static CarOfferSnapshot From(CarOffer offer) => new(
        offer.Name, offer.Variant, offer.DgtLabel, offer.PowerCv,
        offer.CashPriceEur, offer.FinancedPriceEur, offer.ReliabilityScore, offer.ReliabilityText,
        offer.BootLiters, offer.Notes, offer.PriceConfidence, offer.SourceName, offer.SourceUrl,
        offer.LastVerifiedAt, offer.OfferValidUntil, offer.SourcePublishedAt);

    public CarOfferHistory ToHistoryRow(Guid carOfferId, DateTimeOffset recordedAt, string? changedByEmail) => new()
    {
        CarOfferId = carOfferId,
        Name = Name,
        Variant = Variant,
        DgtLabel = DgtLabel,
        PowerCv = PowerCv,
        CashPriceEur = CashPriceEur,
        FinancedPriceEur = FinancedPriceEur,
        ReliabilityScore = ReliabilityScore,
        ReliabilityText = ReliabilityText,
        BootLiters = BootLiters,
        Notes = Notes,
        PriceConfidence = PriceConfidence,
        SourceName = SourceName,
        SourceUrl = SourceUrl,
        LastVerifiedAt = LastVerifiedAt,
        OfferValidUntil = OfferValidUntil,
        SourcePublishedAt = SourcePublishedAt,
        RecordedAt = recordedAt,
        ChangedByEmail = changedByEmail,
    };
}

internal sealed record FinancingOfferSnapshot(
    string Provider, FinancingType Type, decimal? TinPercent, decimal? TaePercent,
    RepaymentStructure RepaymentStructure, string TermDescription, string DownPaymentDescription,
    string FeesDescription, decimal? MonthlyInstallment60Eur, decimal? TotalInterest60Eur, string BestFor,
    Confidence RateConfidence, string? SourceName, string? SourceUrl,
    DateTimeOffset LastVerifiedAt, DateTimeOffset? OfferValidUntil, DateTimeOffset? SourcePublishedAt)
{
    public static FinancingOfferSnapshot From(FinancingOffer offer) => new(
        offer.Provider, offer.Type, offer.TinPercent, offer.TaePercent,
        offer.RepaymentStructure, offer.TermDescription, offer.DownPaymentDescription,
        offer.FeesDescription, offer.MonthlyInstallment60Eur, offer.TotalInterest60Eur, offer.BestFor,
        offer.RateConfidence, offer.SourceName, offer.SourceUrl,
        offer.LastVerifiedAt, offer.OfferValidUntil, offer.SourcePublishedAt);

    public FinancingOfferHistory ToHistoryRow(Guid financingOfferId, DateTimeOffset recordedAt, string? changedByEmail) => new()
    {
        FinancingOfferId = financingOfferId,
        Provider = Provider,
        Type = Type,
        TinPercent = TinPercent,
        TaePercent = TaePercent,
        RepaymentStructure = RepaymentStructure,
        TermDescription = TermDescription,
        DownPaymentDescription = DownPaymentDescription,
        FeesDescription = FeesDescription,
        MonthlyInstallment60Eur = MonthlyInstallment60Eur,
        TotalInterest60Eur = TotalInterest60Eur,
        BestFor = BestFor,
        RateConfidence = RateConfidence,
        SourceName = SourceName,
        SourceUrl = SourceUrl,
        LastVerifiedAt = LastVerifiedAt,
        OfferValidUntil = OfferValidUntil,
        SourcePublishedAt = SourcePublishedAt,
        RecordedAt = recordedAt,
        ChangedByEmail = changedByEmail,
    };
}
