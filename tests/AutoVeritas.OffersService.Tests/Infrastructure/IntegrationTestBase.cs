using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoVeritas.OffersService.Tests.Infrastructure;

/// <summary>
/// One fresh factory (and therefore one fresh database) per test class, so classes
/// can run in parallel without sharing state.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected OffersApiFactory Factory { get; } = new();

    protected HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Client = Factory.CreateClient(new() { AllowAutoRedirect = false });
        await Factory.InitializeDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        Factory.Dispose();
        return Task.CompletedTask;
    }

    protected void AuthenticateAsViewer() =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.ForUser());

    protected void AuthenticateAsAgent() =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.ForAgent());
}
