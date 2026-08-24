using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AutoVeritas.OffersService.Endpoints;

/// <summary>
/// Write-path guards shared by both offer endpoints. They protect the product's
/// trust semantics, which DataAnnotations cannot express.
/// </summary>
public static class OfferWriteGuards
{
    // Generous enough for clock drift between the agent's machine and the server,
    // small enough that "verified tomorrow" cannot make an offer forever Fresh.
    private static readonly TimeSpan ClockDriftTolerance = TimeSpan.FromMinutes(5);

    public static ValidationProblem? FutureVerification(DateTimeOffset? verifiedAt, TimeProvider timeProvider)
    {
        if (verifiedAt is { } value && value > timeProvider.GetUtcNow() + ClockDriftTolerance)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["lastVerifiedAt"] = ["A verification timestamp cannot lie in the future."],
            });
        }

        return null;
    }

    public static ValidationProblem EmptySlug() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["slug"] = ["The name yields no usable slug; supply an explicit slug."],
        });

    public static Conflict<ProblemDetails> DuplicateSlug(string slug) =>
        TypedResults.Conflict(new ProblemDetails
        {
            Title = "Duplicate slug",
            Detail = $"An offer with slug '{slug}' already exists; update it via PUT instead.",
        });
}
