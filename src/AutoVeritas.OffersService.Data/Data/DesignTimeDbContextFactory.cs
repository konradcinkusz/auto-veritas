using AutoVeritas.OffersService.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AutoVeritas.OffersService.Data;

/// <summary>
/// Lets `dotnet ef` build the context without a running application. Migrations are
/// generated from the model, not from a database, so a placeholder connection string
/// is enough when none is configured:
/// <c>DATABASE_PROVIDER=PostgreSQL dotnet ef migrations add X --project src/AutoVeritas.OffersService.Migrations.PostgreSQL --startup-project src/AutoVeritas.OffersService</c>.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OffersDbContext>
{
    public OffersDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var provider = configuration.GetDatabaseProvider();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=offersdb;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<OffersDbContext>();
        DatabaseProviderExtensions.ConfigureProvider(options, connectionString, provider, MigrationsAssemblyFor(provider));
        return new OffersDbContext(options.Options);
    }

    private static string? MigrationsAssemblyFor(DatabaseProviderType provider) => provider switch
    {
        DatabaseProviderType.PostgreSQL => "AutoVeritas.OffersService.Migrations.PostgreSQL",
        _ => null,
    };
}
