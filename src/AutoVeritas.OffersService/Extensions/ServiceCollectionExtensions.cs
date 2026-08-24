using AutoVeritas.OffersService.Data;
using AutoVeritas.OffersService.Domain;
using AutoVeritas.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

namespace AutoVeritas.OffersService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOffersDomain(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<FreshnessCalculator>();
        return services;
    }

    public static IServiceCollection AddOffersPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.RequireSetting("ConnectionStrings:DefaultConnection");
        var provider = configuration.GetDatabaseProvider();
        var migrationsAssembly = configuration.GetMigrationsAssembly();

        services.AddDbContext<OffersDbContext>(options =>
            DatabaseProviderExtensions.ConfigureProvider(options, connectionString, provider, migrationsAssembly));

        services.AddSingleton<IMigrationCompletionSignal, MigrationCompletionSignal>();
        services.AddHostedService<MigrationBackgroundService>();
        services.AddHealthChecks().AddCheck<DatabaseReadyHealthCheck>("database", tags: ["ready"]);

        return services;
    }
}
