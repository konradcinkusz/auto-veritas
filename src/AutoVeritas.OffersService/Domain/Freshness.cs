namespace AutoVeritas.OffersService.Domain;

public enum FreshnessStatus
{
    Fresh,
    Warning,
    Stale,
    Expired,
}

/// <summary>
/// How fast each kind of value goes stale on this market, taken from observed change
/// rates: prices and promotions move weekly, credit rates monthly, technical specs
/// hold for a model's production life. One global TTL would either nag about stable
/// data or trust dead prices — the per-type split is the point.
/// </summary>
public static class FreshnessPolicy
{
    public const int PriceFreshDays = 7;
    public const int PriceWarningDays = 30;

    public const int RateFreshDays = 14;
    public const int RateWarningDays = 45;

    public const int SpecFreshDays = 183;
    public const int SpecWarningDays = 365;
}

public class FreshnessCalculator(TimeProvider timeProvider)
{
    public FreshnessStatus ForPrice(DateTimeOffset lastVerifiedAt, DateTimeOffset? offerValidUntil) =>
        Evaluate(lastVerifiedAt, offerValidUntil, FreshnessPolicy.PriceFreshDays, FreshnessPolicy.PriceWarningDays);

    public FreshnessStatus ForRates(DateTimeOffset lastVerifiedAt, DateTimeOffset? offerValidUntil) =>
        Evaluate(lastVerifiedAt, offerValidUntil, FreshnessPolicy.RateFreshDays, FreshnessPolicy.RateWarningDays);

    public FreshnessStatus ForSpec(DateTimeOffset lastVerifiedAt) =>
        Evaluate(lastVerifiedAt, offerValidUntil: null, FreshnessPolicy.SpecFreshDays, FreshnessPolicy.SpecWarningDays);

    public bool IsExpired(DateTimeOffset? offerValidUntil) =>
        offerValidUntil is { } validUntil && validUntil < timeProvider.GetUtcNow();

    public int DaysSince(DateTimeOffset lastVerifiedAt) =>
        Math.Max(0, (int)(timeProvider.GetUtcNow() - lastVerifiedAt).TotalDays);

    private FreshnessStatus Evaluate(DateTimeOffset lastVerifiedAt, DateTimeOffset? offerValidUntil, int freshDays, int warningDays)
    {
        // A seller-declared expiry beats any verification recency: a price checked
        // yesterday against an offer that ended today is still a dead price.
        if (IsExpired(offerValidUntil))
        {
            return FreshnessStatus.Expired;
        }

        var ageDays = DaysSince(lastVerifiedAt);
        if (ageDays <= freshDays)
        {
            return FreshnessStatus.Fresh;
        }

        return ageDays <= warningDays ? FreshnessStatus.Warning : FreshnessStatus.Stale;
    }
}
