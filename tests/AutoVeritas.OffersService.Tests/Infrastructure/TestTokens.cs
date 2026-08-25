using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AutoVeritas.OffersService.Tests.Infrastructure;

/// <summary>
/// Mints RS256 tokens for tests with a per-run key pair, standing in for the
/// authservice instance. The claim shapes mirror what authservice actually issues:
/// sub/email plus platform roles under the full .NET role claim URI.
/// </summary>
public static class TestTokens
{
    public const string Issuer = "https://auth.auto-veritas.test";
    public const string Audience = "auto-veritas-tests";

    private static readonly RSA Rsa = RSA.Create(2048);

    public static RsaSecurityKey SigningKey { get; } = new(Rsa) { KeyId = "test-signing-key" };

    public static string ForUser(string email = "viewer@example.test", params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, email),
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.RsaSha256),
        });
    }

    public static string ForAgent() => ForUser("agent@example.test", "Admin");
}
