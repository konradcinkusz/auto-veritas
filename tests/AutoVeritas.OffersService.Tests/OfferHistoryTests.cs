using System.Net;
using System.Net.Http.Json;
using AutoVeritas.OffersService.Contracts;
using AutoVeritas.OffersService.Models;
using AutoVeritas.OffersService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AutoVeritas.OffersService.Tests;

public class OfferHistoryTests : IntegrationTestBase
{
    [Fact]
    public async Task Anonymous_requests_to_history_are_rejected_with_401()
    {
        var response = await Client.GetAsync($"/api/v1/car-offers/{Guid.NewGuid()}/history");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_signed_in_viewer_can_read_history_not_only_an_agent()
    {
        AuthenticateAsAgent();
        var id = await CreateCarOfferAsync();

        AuthenticateAsViewer();
        var response = await Client.GetAsync($"/api/v1/car-offers/{id}/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task History_for_a_nonexistent_car_offer_returns_404()
    {
        AuthenticateAsViewer();

        var response = await Client.GetAsync($"/api/v1/car-offers/{Guid.NewGuid()}/history");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Updating_a_car_offer_records_the_previous_price_and_who_changed_it()
    {
        AuthenticateAsAgent();
        var id = await CreateCarOfferAsync(cashPriceEur: 25990);

        var update = ValidCarOfferRequest();
        update.CashPriceEur = 24990;
        await Client.PutAsJsonAsync($"/api/v1/car-offers/{id}", update, Json);

        var history = await Client.GetFromJsonAsync<List<CarOfferHistoryEntryResponse>>($"/api/v1/car-offers/{id}/history", Json);

        var entry = Assert.Single(history!);
        Assert.Equal(25990, entry.CashPriceEur);
        Assert.Equal("agent@example.test", entry.ChangedByEmail);
    }

    [Fact]
    public async Task A_put_that_changes_nothing_creates_no_history_entry()
    {
        AuthenticateAsAgent();
        var id = await CreateCarOfferAsync();
        var offer = await Client.GetFromJsonAsync<CarOfferResponse>($"/api/v1/car-offers/{id}", Json);

        var resend = ValidCarOfferRequest();
        resend.LastVerifiedAt = offer!.LastVerifiedAt;
        await Client.PutAsJsonAsync($"/api/v1/car-offers/{id}", resend, Json);

        var history = await Client.GetFromJsonAsync<List<CarOfferHistoryEntryResponse>>($"/api/v1/car-offers/{id}/history", Json);

        Assert.Empty(history!);
    }

    [Fact]
    public async Task Verifying_a_car_offer_never_creates_a_history_entry()
    {
        AuthenticateAsAgent();
        var id = await CreateCarOfferAsync();

        await Client.PostAsJsonAsync($"/api/v1/car-offers/{id}/verify", new VerifyRequest { VerifiedAt = DateTimeOffset.UtcNow }, Json);

        var history = await Client.GetFromJsonAsync<List<CarOfferHistoryEntryResponse>>($"/api/v1/car-offers/{id}/history", Json);

        Assert.Empty(history!);
    }

    [Fact]
    public async Task Two_updates_come_back_newest_first()
    {
        AuthenticateAsAgent();
        var id = await CreateCarOfferAsync(cashPriceEur: 30000);

        var firstUpdate = ValidCarOfferRequest();
        firstUpdate.CashPriceEur = 28000;
        await Client.PutAsJsonAsync($"/api/v1/car-offers/{id}", firstUpdate, Json);

        var secondUpdate = ValidCarOfferRequest();
        secondUpdate.CashPriceEur = 26000;
        await Client.PutAsJsonAsync($"/api/v1/car-offers/{id}", secondUpdate, Json);

        var history = await Client.GetFromJsonAsync<List<CarOfferHistoryEntryResponse>>($"/api/v1/car-offers/{id}/history", Json);

        Assert.Equal(2, history!.Count);
        Assert.Equal(28000, history[0].CashPriceEur); // the state right before the most recent PUT
        Assert.Equal(30000, history[1].CashPriceEur); // the original state
    }

    [Fact]
    public async Task Deleting_a_car_offer_removes_its_history_too()
    {
        AuthenticateAsAgent();
        var id = await CreateCarOfferAsync(cashPriceEur: 25990);
        var update = ValidCarOfferRequest();
        update.CashPriceEur = 24990;
        await Client.PutAsJsonAsync($"/api/v1/car-offers/{id}", update, Json);

        var deleted = await Client.DeleteAsync($"/api/v1/car-offers/{id}");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        await Factory.WithDbAsync(async db =>
        {
            Assert.False(await db.CarOfferHistories.AnyAsync(row => row.CarOfferId == id));
        });
    }

    [Fact]
    public async Task Updating_a_financing_offer_records_the_previous_rate()
    {
        AuthenticateAsAgent();
        var id = await CreateFinancingOfferAsync(tinPercent: 4.5m);

        var update = ValidFinancingOfferRequest();
        update.TinPercent = 3.9m;
        await Client.PutAsJsonAsync($"/api/v1/financing-offers/{id}", update, Json);

        var history = await Client.GetFromJsonAsync<List<FinancingOfferHistoryEntryResponse>>($"/api/v1/financing-offers/{id}/history", Json);

        var entry = Assert.Single(history!);
        Assert.Equal(4.5m, entry.TinPercent);
    }

    [Fact]
    public async Task Financing_history_for_a_nonexistent_offer_returns_404()
    {
        AuthenticateAsViewer();

        var response = await Client.GetAsync($"/api/v1/financing-offers/{Guid.NewGuid()}/history");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> CreateCarOfferAsync(decimal? cashPriceEur = null)
    {
        var request = ValidCarOfferRequest();
        if (cashPriceEur is not null)
        {
            request.CashPriceEur = cashPriceEur;
        }

        var created = await Client.PostAsJsonAsync("/api/v1/car-offers", request, Json);
        var offer = await created.Content.ReadFromJsonAsync<CarOfferResponse>(Json);
        return offer!.Id;
    }

    private async Task<Guid> CreateFinancingOfferAsync(decimal? tinPercent = null)
    {
        var request = ValidFinancingOfferRequest();
        if (tinPercent is not null)
        {
            request.TinPercent = tinPercent;
        }

        var created = await Client.PostAsJsonAsync("/api/v1/financing-offers", request, Json);
        var offer = await created.Content.ReadFromJsonAsync<FinancingOfferResponse>(Json);
        return offer!.Id;
    }

    private static CarOfferRequest ValidCarOfferRequest() => new()
    {
        Slug = "history-test-car",
        Name = "History Test Car",
        Variant = "SUV / HEV",
        DgtLabel = DgtLabel.Eco,
        PowerCv = 140,
        CashPriceEur = 25990,
        PriceConfidence = Confidence.Confirmed,
        LastVerifiedAt = DateTimeOffset.UtcNow,
    };

    private static FinancingOfferRequest ValidFinancingOfferRequest() => new()
    {
        Slug = "history-test-financing",
        Provider = "History Test Bank",
        Type = FinancingType.Bank,
        TinPercent = 4.5m,
        TaePercent = 4.7m,
        RepaymentStructure = RepaymentStructure.Linear,
        TermDescription = "do 96 mies.",
        DownPaymentDescription = "Brak",
        FeesDescription = "Brak opłat",
        BestFor = "Testowa oferta",
        RateConfidence = Confidence.Confirmed,
        LastVerifiedAt = DateTimeOffset.UtcNow,
    };
}
