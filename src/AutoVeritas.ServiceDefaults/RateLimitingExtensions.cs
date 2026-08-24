using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace AutoVeritas.ServiceDefaults;

/// <summary>
/// The kernel-owned rate limiter: partitioned per authenticated user with a client-IP
/// fallback (never one shared bucket), a generous "api" policy for tagged endpoint
/// groups, a global fallback catching everything nobody tagged, and one uniform
/// 429 body <c>{ error, retryAfter }</c> across all policies.
/// </summary>
public static class RateLimitingExtensions
{
    public const string ApiPolicy = "api";

    private const int ApiPermitLimit = 200;
    private const int GlobalPermitLimit = 500;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddStandardRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(limiter =>
        {
            limiter.AddPolicy(ApiPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(PartitionKey(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = ApiPermitLimit,
                    Window = Window,
                }));

            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(PartitionKey(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = GlobalPermitLimit,
                    Window = Window,
                }));

            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var value)
                    ? value.TotalSeconds
                    : Window.TotalSeconds;
                context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter).ToString();
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { error = "Too many requests.", retryAfter = (int)retryAfter },
                    cancellationToken);
            };
        });

        return services;
    }

    private static string PartitionKey(HttpContext context) =>
        context.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? context.User.FindFirstValue("sub")
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "anonymous";
}
