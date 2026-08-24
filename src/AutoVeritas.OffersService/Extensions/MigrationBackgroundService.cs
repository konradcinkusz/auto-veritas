using AutoVeritas.OffersService.Data;
using AutoVeritas.OffersService.Seeding;

namespace AutoVeritas.OffersService.Extensions;

/// <summary>
/// Applies the schema after Kestrel starts listening (P4): health probes answer while
/// schema work is in flight, and a cold-starting cloud database gets retries instead
/// of a failed deploy. Init failure after the last attempt leaves the service running
/// but never-ready. Seeding failure is logged and tolerated.
/// </summary>
public class MigrationBackgroundService(
    IServiceProvider serviceProvider,
    IMigrationCompletionSignal migrationSignal,
    ILogger<MigrationBackgroundService> logger) : BackgroundService
{
    private const int MaxAttempts = 10;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var db = scope.ServiceProvider.GetRequiredService<OffersDbContext>();
                var mode = scope.ServiceProvider.GetRequiredService<IConfiguration>().GetSchemaMode();
                await DatabaseProviderExtensions.InitializeDatabaseAsync(db, logger, mode, stoppingToken);
                migrationSignal.SetCompleted();

                try
                {
                    await OffersSeeder.SeedAsync(db, stoppingToken);
                }
                catch (Exception seedException) when (seedException is not OperationCanceledException)
                {
                    logger.LogWarning(seedException, "Seeding failed; the service continues with whatever data exists.");
                }

                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                if (attempt == MaxAttempts)
                {
                    logger.LogError(exception,
                        "Database initialization failed after {Attempts} attempts; the service stays up but never becomes ready.",
                        MaxAttempts);
                    return;
                }

                var delay = TimeSpan.FromSeconds(Math.Min(5 * attempt, 30));
                logger.LogWarning(exception,
                    "Database initialization attempt {Attempt}/{Attempts} failed; retrying in {Delay}.",
                    attempt, MaxAttempts, delay);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
