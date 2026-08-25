namespace AutoVeritas.OffersService.Endpoints;

/// <summary>
/// Stable operation ids: generated clients and cross-service references compile
/// against these constants, so a route rename is a visible contract change.
/// </summary>
public static class EndpointNames
{
    public const string ListCarOffers = "ListCarOffers";
    public const string GetCarOffer = "GetCarOffer";
    public const string GetCarOfferHistory = "GetCarOfferHistory";
    public const string CreateCarOffer = "CreateCarOffer";
    public const string UpdateCarOffer = "UpdateCarOffer";
    public const string DeleteCarOffer = "DeleteCarOffer";
    public const string VerifyCarOffer = "VerifyCarOffer";

    public const string ListFinancingOffers = "ListFinancingOffers";
    public const string GetFinancingOffer = "GetFinancingOffer";
    public const string GetFinancingOfferHistory = "GetFinancingOfferHistory";
    public const string CreateFinancingOffer = "CreateFinancingOffer";
    public const string UpdateFinancingOffer = "UpdateFinancingOffer";
    public const string DeleteFinancingOffer = "DeleteFinancingOffer";
    public const string VerifyFinancingOffer = "VerifyFinancingOffer";

    public const string GetFreshnessPolicy = "GetFreshnessPolicy";
}
