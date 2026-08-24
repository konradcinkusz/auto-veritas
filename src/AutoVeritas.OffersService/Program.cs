using System.Text.Json.Serialization;
using AutoVeritas.OffersService.Endpoints;
using AutoVeritas.OffersService.Extensions;
using AutoVeritas.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddStandardRateLimiting();
builder.Services.AddSwaggerWithJwt(
    "AutoVeritas Offers API", "v1",
    "Car and financing offers for the Spanish market, with per-value verification dates. " +
    "Reads require a signed-in user; writes require the Admin or SuperAdmin platform role.");
builder.Services.AddOffersDomain();
builder.Services.AddOffersPersistence(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(options =>
    // allowIntegerValues: false — a numeric enum payload (e.g. "dgtLabel": 7)
    // must fail as a 400, not persist an undefined value.
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false)));

var app = builder.Build();

app.UseSwaggerWhenEnabled();
app.UseCors(CorsExtensions.FrontendPolicy);
// The limiter must see the authenticated principal: registered before
// authentication its per-user partition key is always empty and every request
// shares one bucket keyed by the upstream proxy address.
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapDefaultEndpoints();

// The authorization triad, greppable in one place: viewers read, the owner's agent
// (platform role Admin/SuperAdmin, issued by this system's authservice) writes.
// There is no anonymous product surface — offers require a signed-in user.
var viewerApi = app.MapGroup("/api/v1")
    .RequireAuthorization()
    .RequireRateLimiting(RateLimitingExtensions.ApiPolicy);
var editorApi = app.MapGroup("/api/v1")
    .RequireAuthorization(policy => policy.RequireRole("Admin", "SuperAdmin"))
    .RequireRateLimiting(RateLimitingExtensions.ApiPolicy);

viewerApi.MapCarOfferReadEndpoints();
viewerApi.MapFinancingOfferReadEndpoints();
viewerApi.MapMetaEndpoints();
editorApi.MapCarOfferWriteEndpoints();
editorApi.MapFinancingOfferWriteEndpoints();

app.Run();

// Exposes the entry point to WebApplicationFactory-based integration tests.
public partial class Program
{
}
