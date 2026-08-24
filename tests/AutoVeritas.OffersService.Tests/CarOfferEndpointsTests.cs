using System.Net;
using System.Net.Http.Json;
using AutoVeritas.OffersService.Contracts;
using AutoVeritas.OffersService.Domain;
using AutoVeritas.OffersService.Models;
using AutoVeritas.OffersService.Tests.Infrastructure;

namespace AutoVeritas.OffersService.Tests;

public class CarOfferEndpointsTests : IntegrationTestBase
{
    [Fact]
    public async Task Anonymous_requests_are_rejected_with_401()
    {
        var response = await Client.GetAsync("/api/v1/car-offers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_signed_in_viewer_sees_the_seeded_offers_with_freshness_metadata()
    {
        AuthenticateAsViewer();

        var response = await Client.GetFromJsonAsync<ListResponse<CarOfferResponse>>("/api/v1/car-offers?limit=100", Json);

        Assert.NotNull(response);
        Assert.Equal(17, response.TotalCount);
        var atto2 = Assert.Single(response.Items, offer => offer.Slug == "byd-atto-2-dm-i-active");
        Assert.Equal(5050, atto2.PriceGapEur);
        Assert.NotNull(atto2.OfferValidUntil);
        Assert.True(atto2.DaysSinceVerification >= 0);
        Assert.Equal(Confidence.Confirmed, atto2.PriceConfidence);
    }

    [Fact]
    public async Task A_viewer_cannot_write_offers()
    {
        AuthenticateAsViewer();

        var response = await Client.PostAsJsonAsync("/api/v1/car-offers", ValidRequest(), Json);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_agent_can_create_an_offer_and_read_it_back()
    {
        AuthenticateAsAgent();

        var created = await Client.PostAsJsonAsync("/api/v1/car-offers", ValidRequest(), Json);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var offer = await created.Content.ReadFromJsonAsync<CarOfferResponse>(Json);
        Assert.NotNull(offer);
        Assert.Equal("dacia-duster-hybrid-test", offer.Slug);

        var fetched = await Client.GetFromJsonAsync<CarOfferResponse>($"/api/v1/car-offers/{offer.Id}", Json);
        Assert.NotNull(fetched);
        Assert.Equal("Dacia Duster Hybrid", fetched.Name);
    }

    [Fact]
    public async Task Creating_a_duplicate_slug_returns_409()
    {
        AuthenticateAsAgent();
        var request = ValidRequest();

        var first = await Client.PostAsJsonAsync("/api/v1/car-offers", request, Json);
        var second = await Client.PostAsJsonAsync("/api/v1/car-offers", request, Json);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task A_request_without_a_verification_date_is_rejected_with_400()
    {
        AuthenticateAsAgent();
        var request = ValidRequest();
        request.LastVerifiedAt = null;

        var response = await Client.PostAsJsonAsync("/api/v1/car-offers", request, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Verify_touches_the_verification_timestamp_without_changing_values()
    {
        AuthenticateAsAgent();
        var offers = await Client.GetFromJsonAsync<ListResponse<CarOfferResponse>>("/api/v1/car-offers?search=atto 2 dm-i active", Json);
        var atto2 = Assert.Single(offers!.Items);
        var newTimestamp = DateTimeOffset.UtcNow;

        var response = await Client.PostAsJsonAsync(
            $"/api/v1/car-offers/{atto2.Id}/verify", new VerifyRequest { VerifiedAt = newTimestamp }, Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<CarOfferResponse>(Json);
        Assert.NotNull(updated);
        Assert.Equal(newTimestamp, updated.LastVerifiedAt, TimeSpan.FromSeconds(1));
        Assert.Equal(atto2.CashPriceEur, updated.CashPriceEur);
    }

    [Fact]
    public async Task The_freshness_filter_hides_offers_verified_before_the_cutoff()
    {
        AuthenticateAsAgent();
        var stale = ValidRequest();
        stale.Slug = "stale-offer-test";
        stale.Name = "Stale Offer";
        stale.LastVerifiedAt = DateTimeOffset.UtcNow.AddDays(-90);
        await Client.PostAsJsonAsync("/api/v1/car-offers", stale, Json);

        var filtered = await Client.GetFromJsonAsync<ListResponse<CarOfferResponse>>(
            "/api/v1/car-offers?maxVerifiedAgeDays=30&limit=100", Json);
        var unfiltered = await Client.GetFromJsonAsync<ListResponse<CarOfferResponse>>(
            "/api/v1/car-offers?limit=100", Json);

        Assert.DoesNotContain(filtered!.Items, offer => offer.Slug == "stale-offer-test");
        Assert.Contains(unfiltered!.Items, offer => offer.Slug == "stale-offer-test");
    }

    [Fact]
    public async Task The_label_filter_returns_only_matching_offers()
    {
        AuthenticateAsViewer();

        var response = await Client.GetFromJsonAsync<ListResponse<CarOfferResponse>>(
            "/api/v1/car-offers?label=Eco&limit=100", Json);

        Assert.NotEmpty(response!.Items);
        Assert.All(response.Items, offer => Assert.Equal(DgtLabel.Eco, offer.DgtLabel));
    }

    [Fact]
    public async Task An_offer_past_its_declared_validity_is_marked_expired_but_still_listed()
    {
        AuthenticateAsAgent();
        var expired = ValidRequest();
        expired.Slug = "expired-offer-test";
        expired.Name = "Expired Offer";
        expired.OfferValidUntil = DateTimeOffset.UtcNow.AddDays(-2);
        await Client.PostAsJsonAsync("/api/v1/car-offers", expired, Json);

        var offers = await Client.GetFromJsonAsync<ListResponse<CarOfferResponse>>("/api/v1/car-offers?limit=100", Json);

        var offer = Assert.Single(offers!.Items, o => o.Slug == "expired-offer-test");
        Assert.True(offer.IsExpired);
        Assert.Equal(FreshnessStatus.Expired, offer.PriceFreshness);
    }

    private static CarOfferRequest ValidRequest() => new()
    {
        Slug = "dacia-duster-hybrid-test",
        Name = "Dacia Duster Hybrid",
        Variant = "SUV / HEV",
        DgtLabel = DgtLabel.Eco,
        PowerCv = 140,
        CashPriceEur = 24500,
        PriceConfidence = Confidence.Confirmed,
        LastVerifiedAt = DateTimeOffset.UtcNow,
    };
}
