using System.Net;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using Padel.Infrastructure.Persistence;
using Padel.Infrastructure.Persistence.Outbox;
using Padel.IntegrationTests.Infrastructure;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class HealthCheckDependencyTests(PadelApiFactory factory)
{
    [Test]
    public async Task Readiness_GivenDatabaseWithoutMigrations_ShouldBeUnavailable()
    {
        // A new, empty database on the same server: reachable, but the migrations have not run.
        var emptyDatabase = await CreateEmptyDatabaseAsync();
        await using var api = factory.WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:padel", emptyDatabase));
        using var client = api.CreateClient();

        using var response = await client.GetAsync("health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Test]
    public async Task OutboxCheck_GivenMessageThatFailedEveryAttempt_ShouldBeDegraded()
    {
        await using (var context = new PadelDbContext(new DbContextOptionsBuilder<PadelDbContext>().UseNpgsql(factory.DatabaseConnectionString).Options))
        {
            context.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.CreateVersion7(),
                Type = "Padel.Domain.Test",
                Payload = "{}",
                OccurredOn = DateTimeOffset.UtcNow,
                Attempts = 5,
                LastError = "The mail server is down.",
            });
            await context.SaveChangesAsync();
        }

        var report = await factory.Services.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(check => check.Name == "outbox");

        var outbox = report.Entries["outbox"];
        outbox.Status.Should().Be(HealthStatus.Degraded);
        outbox.Description.Should().Contain("no longer retried");
        ((int)outbox.Data["failedMessages"]).Should().BeGreaterThan(0);
    }

    private async Task<string> CreateEmptyDatabaseAsync()
    {
        var name = $"empty_{Guid.NewGuid():N}";
        await using (var connection = new NpgsqlConnection(factory.DatabaseConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection);
            await command.ExecuteNonQueryAsync();
        }

        return new NpgsqlConnectionStringBuilder(factory.DatabaseConnectionString) { Database = name }.ConnectionString;
    }
}
