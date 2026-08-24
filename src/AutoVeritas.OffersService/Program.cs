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
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.UseSwaggerWhenEnabled();
app.UseCors(CorsExtensions.FrontendPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

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
