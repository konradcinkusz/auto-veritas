// The composition root (P1): every resource the system needs, declared once, so a
// developer clones the repository and runs one command. This is a development
// topology; production is described by flyio/*.fly.toml, never generated from here.
//
// authservice is consumed as its published container image — its source is never
// part of this system. The RS256 dev signing key is a local secret:
//   dotnet user-secrets set Parameters:jwt-signing-key "$(cat certs/jwt-signing.dev.pem)" --project src/AutoVeritas.AppHost
// after scripts/generate-jwt-key.sh (or .ps1) writes the PEM.

var builder = DistributedApplication.CreateBuilder(args);

var jwtSigningKey = builder.AddParameter("jwt-signing-key", secret: true);

var postgres = builder.AddPostgres("postgres");
var authDb = postgres.AddDatabase("authdb");
var offersDb = postgres.AddDatabase("offersdb");

var authservice = builder.AddContainer("authservice", "ghcr.io/konradcinkusz/authservice", "v0.3.1")
    .WithHttpEndpoint(port: 8081, targetPort: 8080)
    .WithReference(authDb)
    .WaitFor(authDb)
    .WithEnvironment("ConnectionStrings__DefaultConnection", authDb)
    .WithEnvironment("DatabaseProvider", "PostgreSQL")
    .WithEnvironment("Database__SchemaMode", "EnsureCreated")
    .WithEnvironment("Jwt__PrivateKeyPem", jwtSigningKey)
    .WithEnvironment("Jwt__Issuer", "https://auth.auto-veritas.local")
    .WithEnvironment("Jwt__Audience", "auto-veritas")
    .WithEnvironment("Jwt__PublicBaseUrl", "http://localhost:8081")
    .WithEnvironment("Cors__AllowedOrigins__0", "http://localhost:3000")
    .WithEnvironment("InitialAdmin__Email", "admin@auto-veritas.local")
    .WithEnvironment("InitialAdmin__Password", "Admin123!")
    .WithHttpHealthCheck("/health/ready", endpointName: "http");

var offers = builder.AddProject<Projects.AutoVeritas_OffersService>("offers")
    .WithReference(offersDb)
    .WaitFor(offersDb)
    .WithEnvironment("ConnectionStrings__DefaultConnection", offersDb)
    .WithEnvironment("Jwt__MetadataAddress", "http://localhost:8081/.well-known/openid-configuration")
    .WithEnvironment("Jwt__Issuer", "https://auth.auto-veritas.local")
    .WithEnvironment("Jwt__Audience", "auto-veritas")
    .WithEnvironment("Cors__AllowedOrigins__0", "http://localhost:3000")
    .WithHttpHealthCheck("/health");

builder.AddNextJsApp("web", "../../apps/web")
    .WithHttpEndpoint(port: 3000, targetPort: 3000)
    .WithEnvironment("AUTH_URL", "http://localhost:8081")
    .WithEnvironment("AUTH_JWKS_URL", "http://localhost:8081/.well-known/jwks.json")
    .WithEnvironment("AUTH_ISSUER", "https://auth.auto-veritas.local")
    .WithEnvironment("AUTH_AUDIENCE", "auto-veritas")
    .WithReference(offers)
    .WaitFor(offers);

builder.Build().Run();
