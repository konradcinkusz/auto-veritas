using AutoVeritas.OffersService.Domain;

namespace AutoVeritas.OffersService.Tests;

public class FreshnessCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    private sealed class StubTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static FreshnessCalculator Calculator() => new(new StubTimeProvider(Now));

    [Theory]
    [InlineData(0, FreshnessStatus.Fresh)]
    [InlineData(7, FreshnessStatus.Fresh)]
    [InlineData(8, FreshnessStatus.Warning)]
    [InlineData(30, FreshnessStatus.Warning)]
    [InlineData(31, FreshnessStatus.Stale)]
    public void Price_freshness_follows_the_7_and_30_day_thresholds(int ageDays, FreshnessStatus expected)
    {
        var status = Calculator().ForPrice(Now.AddDays(-ageDays), offerValidUntil: null);

        Assert.Equal(expected, status);
    }

    [Theory]
    [InlineData(14, FreshnessStatus.Fresh)]
    [InlineData(15, FreshnessStatus.Warning)]
    [InlineData(45, FreshnessStatus.Warning)]
    [InlineData(46, FreshnessStatus.Stale)]
    public void Rate_freshness_follows_the_14_and_45_day_thresholds(int ageDays, FreshnessStatus expected)
    {
        var status = Calculator().ForRates(Now.AddDays(-ageDays), offerValidUntil: null);

        Assert.Equal(expected, status);
    }

    [Theory]
    [InlineData(183, FreshnessStatus.Fresh)]
    [InlineData(200, FreshnessStatus.Warning)]
    [InlineData(400, FreshnessStatus.Stale)]
    public void Spec_freshness_follows_the_six_and_twelve_month_thresholds(int ageDays, FreshnessStatus expected)
    {
        var status = Calculator().ForSpec(Now.AddDays(-ageDays));

        Assert.Equal(expected, status);
    }

    [Fact]
    public void A_seller_expiry_in_the_past_beats_a_verification_from_yesterday()
    {
        var status = Calculator().ForPrice(Now.AddDays(-1), offerValidUntil: Now.AddDays(-1));

        Assert.Equal(FreshnessStatus.Expired, status);
    }

    [Fact]
    public void A_future_seller_expiry_leaves_the_recency_verdict_untouched()
    {
        var status = Calculator().ForPrice(Now.AddDays(-1), offerValidUntil: Now.AddDays(30));

        Assert.Equal(FreshnessStatus.Fresh, status);
    }

    [Fact]
    public void Days_since_verification_never_goes_negative()
    {
        var days = Calculator().DaysSince(Now.AddDays(3));

        Assert.Equal(0, days);
    }
}
