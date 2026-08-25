using AutoVeritas.OffersService.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AutoVeritas.OffersService.Extensions;

/// <summary>
/// Readiness = schema initialization finished AND the database answers. Liveness
/// stays a separate, static question — see MapDefaultEndpoints in the kernel.
/// </summary>
public class DatabaseReadyHealthCheck(OffersDbContext db, IMigrationCompletionSignal migrationSignal) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!migrationSignal.IsCompleted)
        {
            return HealthCheckResult.Unhealthy("Schema initialization has not completed.");
        }

        return await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy("Database is reachable and schema is initialized.")
            : HealthCheckResult.Unhealthy("Database is unreachable.");
    }
}
