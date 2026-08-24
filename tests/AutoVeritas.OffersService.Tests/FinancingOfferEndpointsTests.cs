using System.Net;
using System.Net.Http.Json;
using AutoVeritas.OffersService.Contracts;
using AutoVeritas.OffersService.Models;
using AutoVeritas.OffersService.Tests.Infrastructure;

namespace AutoVeritas.OffersService.Tests;

public class FinancingOfferEndpointsTests : IntegrationTestBase
{
    [Fact]
    public async Task Anonymous_requests_are_rejected_with_401()
    {
        var response = await Client.GetAsync("/api/v1/financing-offers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_signed_in_viewer_sees_all_seeded_financing_offers()
    {
        AuthenticateAsViewer();

        var response = await Client.GetFromJsonAsync<ListResponse<FinancingOfferResponse>>(
            "/api/v1/financing-offers?limit=100", Json);

        Assert.NotNull(response);
        Assert.Equal(22, response.TotalCount);
    }

    [Fact]
    public async Task Subscriptions_without_a_tin_always_pass_the_rate_filter()
    {
        AuthenticateAsViewer();

        var response = await Client.GetFromJsonAsync<ListResponse<FinancingOfferResponse>>(
            "/api/v1/financing-offers?maxTin=4.0&limit=100", Json);

        Assert.Contains(response!.Items, offer => offer.Type == FinancingType.Subscription);
        Assert.All(response.Items, offer => Assert.True(offer.TinPercent is null || offer.TinPercent <= 4.0m));
    }

    [Fact]
    public async Task The_repayment_structure_is_exposed_as_a_first_class_attribute()
    {
        AuthenticateAsViewer();

        var response = await Client.GetFromJsonAsync<ListResponse<FinancingOfferResponse>>(
            "/api/v1/financing-offers?search=toyota easy", Json);

        var toyotaEasy = Assert.Single(response!.Items);
        Assert.Equal(RepaymentStructure.Balloon, toyotaEasy.RepaymentStructure);
    }

    [Fact]
    public async Task A_viewer_cannot_write_financing_offers()
    {
        AuthenticateAsViewer();

        var response = await Client.PostAsJsonAsync("/api/v1/financing-offers", ValidRequest(), Json);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_agent_can_create_update_and_delete_a_financing_offer()
    {
        AuthenticateAsAgent();

        var created = await Client.PostAsJsonAsync("/api/v1/financing-offers", ValidRequest(), Json);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var offer = await created.Content.ReadFromJsonAsync<FinancingOfferResponse>(Json);
        Assert.NotNull(offer);

        var update = ValidRequest();
        update.TinPercent = 4.99m;
        var updated = await Client.PutAsJsonAsync($"/api/v1/financing-offers/{offer.Id}", update, Json);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var updatedOffer = await updated.Content.ReadFromJsonAsync<FinancingOfferResponse>(Json);
        Assert.Equal(4.99m, updatedOffer!.TinPercent);

        var deleted = await Client.DeleteAsync($"/api/v1/financing-offers/{offer.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        var gone = await Client.GetAsync($"/api/v1/financing-offers/{offer.Id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task A_payload_omitting_the_repayment_structure_is_rejected_with_400()
    {
        AuthenticateAsAgent();

        var response = await Client.PostAsJsonAsync("/api/v1/financing-offers", new
        {
            provider = "Structureless Bank",
            type = "Bank",
            termDescription = "60 mies.",
            downPaymentDescription = "Brak",
            feesDescription = "Brak",
            bestFor = "Test",
            rateConfidence = "Confirmed",
            lastVerifiedAt = DateTimeOffset.UtcNow,
        }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Updating_to_an_already_taken_slug_returns_409()
    {
        AuthenticateAsAgent();
        var created = await Client.PostAsJsonAsync("/api/v1/financing-offers", ValidRequest(), Json);
        var offer = await created.Content.ReadFromJsonAsync<FinancingOfferResponse>(Json);

        var update = ValidRequest();
        update.Slug = "bankinter-consumer-finance";
        var response = await Client.PutAsJsonAsync($"/api/v1/financing-offers/{offer!.Id}", update, Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task The_freshness_policy_endpoint_publishes_the_thresholds()
    {
        AuthenticateAsViewer();

        var policy = await Client.GetFromJsonAsync<FreshnessPolicyResponse>("/api/v1/meta/freshness-policy", Json);

        Assert.NotNull(policy);
        Assert.Equal(7, policy.PriceFreshDays);
        Assert.Equal(45, policy.RateWarningDays);
    }

    private static FinancingOfferRequest ValidRequest() => new()
    {
        Slug = "test-bank-credit",
        Provider = "Test Bank",
        Type = FinancingType.Bank,
        TinPercent = 5.10m,
        TaePercent = 5.35m,
        RepaymentStructure = RepaymentStructure.Linear,
        TermDescription = "do 96 mies.",
        DownPaymentDescription = "Brak",
        FeesDescription = "Brak opłat",
        BestFor = "Testowa oferta",
        RateConfidence = Confidence.Confirmed,
        LastVerifiedAt = DateTimeOffset.UtcNow,
    };
}
