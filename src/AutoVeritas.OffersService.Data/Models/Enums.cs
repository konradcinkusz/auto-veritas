namespace AutoVeritas.OffersService.Models;

/// <summary>Spanish DGT environmental label; decides urban low-emission-zone access.</summary>
public enum DgtLabel
{
    Cero,
    Eco,
    C,
    B,
}

public enum FinancingType
{
    Bank,
    Green,
    Fintech,
    Manufacturer,
    Subscription,
}

/// <summary>
/// How the financing amortizes. Linear means the car is fully owned after the last
/// installment with no surprises; Balloon means a low monthly plus a large final
/// payment that advertising tends to bury.
/// </summary>
public enum RepaymentStructure
{
    Linear,
    Balloon,
    Subscription,
    Unknown,
}

/// <summary>
/// Whether the agent confirmed the value at the source or estimated it. Estimates are
/// shown as such ("szacunek — do potwierdzenia u dealera") instead of being passed
/// off as facts; that asymmetry is the product's trust mechanism.
/// </summary>
public enum Confidence
{
    Confirmed,
    Estimated,
}
