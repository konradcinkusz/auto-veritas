using System.Security.Claims;

namespace AutoVeritas.OffersService.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// authservice emits the short JWT claim name "email"; depending on the
    /// token handler's inbound claim mapping it can surface as either that
    /// literal string or the long-form <see cref="ClaimTypes.Email"/> URI —
    /// the same defensive both-spellings pattern <c>RateLimitingExtensions</c>
    /// already uses for the subject claim.
    /// </summary>
    public static string? GetEmail(this ClaimsPrincipal user) =>
        user.FindFirstValue("email") ?? user.FindFirstValue(ClaimTypes.Email);
}
