using AutoVeritas.OffersService.Data;
using AutoVeritas.OffersService.Extensions;
using AutoVeritas.OffersService.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace AutoVeritas.OffersService.Tests.Infrastructure;

public class OffersApiFactory : WebApplicationFactory<Program>
{
    // Program.cs validates configuration in top-level statements, before
    // ConfigureAppConfiguration delegates run, so required settings must arrive
    // as environment variables before the host is ever built.
    static OffersApiFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("Jwt__Issuer", TestTokens.Issuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", TestTokens.Audience);
        Environment.SetEnvironmentVariable("Jwt__MetadataAddress", "");
        // Placeholder only: the SQLite context below replaces the registration.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "DataSource=:memory:");
        Environment.SetEnvironmentVariable("Swagger__Enabled", "false");
    }

    // The in-memory database lives exactly as long as this connection stays open.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // SQLite over the EF InMemory provider: InMemory silently ignores the
            // unique slug index these tests exist to verify. Removing only the
            // options descriptors is not enough — EF stores the AddDbContext
            // callback as its own DbContextOptionsConfiguration service, and
            // leaving it applies UseNpgsql alongside UseSqlite.
            foreach (var descriptor in services.Where(d =>
                         d.ServiceType == typeof(OffersDbContext)
                         || d.ServiceType == typeof(DbContextOptions)
                         || d.ServiceType == typeof(DbContextOptions<OffersDbContext>)
                         || d.ServiceType.FullName?.Contains("DbContextOptionsConfiguration") == true).ToList())
            {
                services.Remove(descriptor);
            }

            _connection.Open();
            services.AddDbContext<OffersDbContext>(options => options.UseSqlite(_connection));

            // Schema and seeding are applied synchronously in InitializeAsync; the
            // migration hosted service would race it. Filter by assembly — the test
            // server itself is a hosted service and must survive.
            foreach (var descriptor in services.Where(d =>
                         d.ServiceType == typeof(IHostedService)
                         && d.ImplementationType?.Assembly == typeof(Program).Assembly).ToList())
            {
                services.Remove(descriptor);
            }

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.IssuerSigningKey = TestTokens.SigningKey;
            });
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OffersDbContext>();
        await db.Database.EnsureCreatedAsync();
        await OffersSeeder.SeedAsync(db);
        Services.GetRequiredService<IMigrationCompletionSignal>().SetCompleted();
    }

    public async Task WithDbAsync(Func<OffersDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<OffersDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
