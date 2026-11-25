using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Padel.Application.Common.Notifications;
using Testcontainers.Keycloak;
using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

namespace Padel.IntegrationTests.Infrastructure;

/// <summary>
/// Starts one PostgreSQL container, one Keycloak container (with the realm from the AppHost) and one
/// API host for the whole test session. The API applies the EF Core migrations on start-up, as in Development.
/// </summary>
public sealed class PadelApiFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    private const string KeycloakAdminUsername = "admin";
    private const string KeycloakAdminPassword = "admin";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine").Build();

    // Same image as the Aspire AppHost uses.
    private readonly KeycloakContainer _keycloak = new KeycloakBuilder("quay.io/keycloak/keycloak:26.8")
        .WithUsername(KeycloakAdminUsername)
        .WithPassword(KeycloakAdminPassword)
        .WithResourceMapping(
            FilePath.Of(Path.Combine(AppContext.BaseDirectory, "Keycloak", "padel-realm.json")),
            DirectoryPath.Of("/opt/keycloak/data/import/"))
        .WithCommand("--import-realm")
        .Build();

    private HttpClient? _keycloakClient;

    public KeycloakTokens Tokens { get; private set; } = null!;

    /// <summary>Notifications the API sent (through the outbox).</summary>
    public RecordingNotificationSender Notifications { get; } = new();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_database.StartAsync(), _keycloak.StartAsync());

        _keycloakClient = new HttpClient { BaseAddress = new Uri(_keycloak.GetBaseAddress()) };
        Tokens = new KeycloakTokens(_keycloakClient, KeycloakAdminUsername, KeycloakAdminPassword);

        // Accessing the server starts the host, which runs the migrations.
        _ = Server;
    }

    /// <summary>Typed clients for one caller. The token is fetched lazily, on the first request.</summary>
    public PadelApi CreateApi(Func<Task<string>>? token = null) =>
        new(token is null
            ? CreateDefaultClient(new RedirectHandler())
            : CreateDefaultClient(new RedirectHandler(), new BearerTokenHandler(token)));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:padel", _database.GetConnectionString());
        builder.UseSetting("Keycloak:Authority", Tokens.Authority);
        builder.UseSetting("Keycloak:RequireHttpsMetadata", "false");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("OpenApi:Enabled", "true");
        builder.UseSetting("Outbox:PollingInterval", "00:00:00.100");

        builder.ConfigureTestServices(services => services.AddSingleton<INotificationSender>(Notifications));
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        _keycloakClient?.Dispose();
        await Task.WhenAll(_database.DisposeAsync().AsTask(), _keycloak.DisposeAsync().AsTask());
    }

    private sealed class BearerTokenHandler(Func<Task<string>> token) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new("Bearer", await token());
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
