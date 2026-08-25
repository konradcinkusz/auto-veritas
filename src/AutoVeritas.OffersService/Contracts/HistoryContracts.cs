using AutoVeritas.OffersService.Models;

namespace AutoVeritas.OffersService.Contracts;

/// <summary>A prior version of a car offer, as it stood before a later PUT changed it.</summary>
public record CarOfferHistoryEntryResponse(
    Guid Id,
    DateTimeOffset RecordedAt,
    string? ChangedByEmail,
    string Name,
    string Variant,
    DgtLabel DgtLabel,
    int PowerCv,
    decimal? CashPriceEur,
    decimal? FinancedPriceEur,
    int? ReliabilityScore,
    string? ReliabilityText,
    int? BootLiters,
    string? Notes,
    Confidence PriceConfidence,
    string? SourceName,
    string? SourceUrl,
    DateTimeOffset LastVerifiedAt,
    DateTimeOffset? OfferValidUntil,
    DateTimeOffset? SourcePublishedAt)
{
    public static CarOfferHistoryEntryResponse From(CarOfferHistory row) => new(
        row.Id, row.RecordedAt, row.ChangedByEmail,
        row.Name, row.Variant, row.DgtLabel, row.PowerCv,
        row.CashPriceEur, row.FinancedPriceEur, row.ReliabilityScore, row.ReliabilityText,
        row.BootLiters, row.Notes, row.PriceConfidence, row.SourceName, row.SourceUrl,
        row.LastVerifiedAt, row.OfferValidUntil, row.SourcePublishedAt);
}

/// <summary>A prior version of a financing offer, as it stood before a later PUT changed it.</summary>
public record FinancingOfferHistoryEntryResponse(
    Guid Id,
    DateTimeOffset RecordedAt,
    string? ChangedByEmail,
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
    DateTimeOffset? SourcePublishedAt)
{
    public static FinancingOfferHistoryEntryResponse From(FinancingOfferHistory row) => new(
        row.Id, row.RecordedAt, row.ChangedByEmail,
        row.Provider, row.Type, row.TinPercent, row.TaePercent, row.RepaymentStructure,
        row.TermDescription, row.DownPaymentDescription, row.FeesDescription,
        row.MonthlyInstallment60Eur, row.TotalInterest60Eur, row.BestFor, row.RateConfidence,
        row.SourceName, row.SourceUrl, row.LastVerifiedAt, row.OfferValidUntil, row.SourcePublishedAt);
}
