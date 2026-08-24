using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AutoVeritas.ServiceDefaults;

/// <summary>
/// Verify-only JWT authentication against the system's authservice instance (P5).
/// This kernel holds no key material: signing keys are fetched from the OIDC
/// discovery document named by <c>Jwt:MetadataAddress</c>, and the authservice
/// instance is the only holder of the private key.
/// </summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var issuer = configuration.RequireSetting("Jwt:Issuer");
        var audience = configuration.RequireSetting("Jwt:Audience");
        var metadataAddress = configuration["Jwt:MetadataAddress"];

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // authservice's discovery document lives at
                // <base>/.well-known/openid-configuration. Its issuer is the configured
                // string, not the discovery URL, so MetadataAddress + explicit
                // ValidIssuer is the supported wiring (never Authority-derived).
                if (!string.IsNullOrWhiteSpace(metadataAddress))
                {
                    options.MetadataAddress = metadataAddress;
                    options.RequireHttpsMetadata = metadataAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
                }

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    // authservice validates its own tokens with zero skew; a laxer
                    // consumer would accept tokens the issuer itself already rejects.
                    ClockSkew = TimeSpan.Zero,
                };
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Reads a required configuration value, failing startup with a message that names
    /// the setting and how to supply it. Keys ship present-but-empty in appsettings.json,
    /// so the check is IsNullOrWhiteSpace, never a null check.
    /// </summary>
    public static string RequireSetting(this IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            var environmentVariable = key.Replace(":", "__");
            throw new InvalidOperationException(
                $"Configuration value '{key}' is required. Set it in appsettings.json, " +
                $"via 'dotnet user-secrets set \"{key}\" <value>', or as the environment variable {environmentVariable}.");
        }

        return value;
    }
}
