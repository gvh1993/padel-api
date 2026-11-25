using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Padel.Application.Common.Notifications;
using Padel.Infrastructure.Persistence;
using Testcontainers.Keycloak;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using TUnit.Core.Interfaces;

namespace Padel.IntegrationTests.Infrastructure;

/// <summary>
/// Starts one PostgreSQL container, one Redis container (rate limits), one Keycloak container (with the realm from the AppHost) and one
/// API host for the whole test session. The database is migrated before the API starts.
/// </summary>
public sealed class PadelApiFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    private const string KeycloakAdminUsername = "admin";
    private const string KeycloakAdminPassword = "admin";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine").Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:8-alpine").Build();

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

    /// <summary>The test database, for tests that look at the schema itself.</summary>
    public string DatabaseConnectionString => _database.GetConnectionString();

    /// <summary>Notifications the API sent (through the outbox).</summary>
    public RecordingNotificationSender Notifications { get; } = new();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_database.StartAsync(), _redis.StartAsync(), _keycloak.StartAsync());

        _keycloakClient = new HttpClient { BaseAddress = new Uri(_keycloak.GetBaseAddress()) };
        Tokens = new KeycloakTokens(_keycloakClient, KeycloakAdminUsername, KeycloakAdminPassword);

        // As in production, the schema is migrated before the API starts (here without the migration service).
        await using (var context = new PadelDbContext(new DbContextOptionsBuilder<PadelDbContext>().UseNpgsql(_database.GetConnectionString()).Options))
        {
            await context.Database.MigrateAsync();
        }

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
        builder.UseSetting("ConnectionStrings:redis", _redis.GetConnectionString());
        builder.UseSetting("Keycloak:Authority", Tokens.Authority);
        builder.UseSetting("Keycloak:RequireHttpsMetadata", "false");
        builder.UseSetting("OpenApi:Enabled", "true");
        builder.UseSetting("Outbox:PollingInterval", "00:00:00.100");
        builder.UseSetting("Outbox:CleanupInterval", "00:00:00.200");

        // Tests send many requests per account; RateLimitingTests uses its own, low limits.
        builder.UseSetting("RateLimiting:PermitsPerMinute", "100000");
        builder.UseSetting("RateLimiting:BookingPermitsPerMinute", "100000");

        builder.ConfigureTestServices(services => services.AddSingleton<INotificationSender>(Notifications));
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        _keycloakClient?.Dispose();
        await Task.WhenAll(_database.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask(), _keycloak.DisposeAsync().AsTask());
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
