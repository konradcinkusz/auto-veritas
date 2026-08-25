using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AutoVeritas.OffersService.Extensions;

public enum DatabaseProviderType
{
    PostgreSQL,
    SqlServer,
}

public enum SchemaInitializationMode
{
    Migrate,
    EnsureCreated,
    None,
}

/// <summary>
/// Runtime-selectable database provider (P4): the provider is a configuration switch,
/// the schema is applied by migrations, and EnsureCreated survives only as the
/// bootstrap for tests and demos.
/// </summary>
public static class DatabaseProviderExtensions
{
    public static DatabaseProviderType GetDatabaseProvider(this IConfiguration configuration)
    {
        var value = configuration["DATABASE_PROVIDER"] ?? configuration["DatabaseProvider"];
        return value?.Trim().ToLowerInvariant() switch
        {
            "sqlserver" or "mssql" => DatabaseProviderType.SqlServer,
            _ => DatabaseProviderType.PostgreSQL,
        };
    }

    public static SchemaInitializationMode GetSchemaMode(this IConfiguration configuration)
    {
        var value = configuration["Database:SchemaMode"] ?? configuration["DATABASE_SCHEMA_MODE"];
        return value?.Trim().ToLowerInvariant() switch
        {
            "ensurecreated" => SchemaInitializationMode.EnsureCreated,
            "none" => SchemaInitializationMode.None,
            _ => SchemaInitializationMode.Migrate,
        };
    }

    public static string? GetMigrationsAssembly(this IConfiguration configuration)
    {
        var value = configuration["Database:MigrationsAssembly"];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public static void ConfigureProvider(DbContextOptionsBuilder options, string connectionString, DatabaseProviderType provider, string? migrationsAssembly = null)
    {
        switch (provider)
        {
            case DatabaseProviderType.PostgreSQL:
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.EnableRetryOnFailure(maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
                    npgsql.CommandTimeout(60);
                    if (migrationsAssembly is not null)
                    {
                        npgsql.MigrationsAssembly(migrationsAssembly);
                    }
                });
                break;
            case DatabaseProviderType.SqlServer:
                throw new NotSupportedException(
                    "SqlServer is reserved as a future provider; generate a migration set under " +
                    "src/AutoVeritas.OffersService.Migrations.SqlServer before enabling it.");
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown database provider.");
        }
    }

    public static async Task InitializeDatabaseAsync<TContext>(TContext context, ILogger logger, SchemaInitializationMode mode, CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        switch (mode)
        {
            case SchemaInitializationMode.Migrate:
                await context.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Database migrations applied.");
                break;
            case SchemaInitializationMode.EnsureCreated:
                var created = await context.Database.EnsureCreatedAsync(cancellationToken);
                if (!created)
                {
                    logger.LogWarning(
                        "EnsureCreated found an existing database and applied NOTHING. It has no upgrade " +
                        "path; use SchemaMode=Migrate anywhere the schema is expected to evolve.");
                }
                break;
            case SchemaInitializationMode.None:
                logger.LogInformation("Schema initialization skipped (SchemaMode=None); DDL is managed out of band.");
                break;
        }
    }
}
