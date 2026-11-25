using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Padel.IntegrationTests.Infrastructure;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class RateLimitingTests(PadelApiFactory factory)
{
    [Test]
    public async Task ListCourts_WhenMoreRequestsThanTheLimit_ShouldReturn429ProblemWithRetryAfter()
    {
        await using var limited = WithLimitOfTwo(UniquePrefix());
        var client = await CreateClientAsync(limited);

        (await client.GetAsync("api/v1/courts")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("api/v1/courts")).StatusCode.Should().Be(HttpStatusCode.OK);
        var rejected = await client.GetAsync("api/v1/courts");

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejected.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        rejected.Headers.RetryAfter.Should().NotBeNull();
        (await client.GetAsync("health/live")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task ListCourts_WhenSpreadOverTwoInstances_ShouldShareTheLimitThroughRedis()
    {
        var prefix = UniquePrefix();
        await using var first = WithLimitOfTwo(prefix);
        await using var second = WithLimitOfTwo(prefix);
        var firstClient = await CreateClientAsync(first);
        var secondClient = await CreateClientAsync(second);

        (await firstClient.GetAsync("api/v1/courts")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await secondClient.GetAsync("api/v1/courts")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await firstClient.GetAsync("api/v1/courts")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await secondClient.GetAsync("api/v1/courts")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task ListCourts_WhenRedisIsUnavailable_ShouldNotLimit()
    {
        await using var withoutRedis = factory.WithWebHostBuilder(builder => builder
            .UseSetting("ConnectionStrings:redis", "localhost:1,abortConnect=false,connectTimeout=200,asyncTimeout=500,syncTimeout=500")
            .UseSetting("RateLimiting:PermitsPerMinute", "1"));
        var client = await CreateClientAsync(withoutRedis);

        (await client.GetAsync("api/v1/courts")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("api/v1/courts")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>The same containers and database, but an API host with a limit of 2 requests per minute.</summary>
    private WebApplicationFactory<Program> WithLimitOfTwo(string keyPrefix) =>
        factory.WithWebHostBuilder(builder => builder
            .UseSetting("RateLimiting:PermitsPerMinute", "2")
            .UseSetting("RateLimiting:KeyPrefix", keyPrefix));

    private async Task<HttpClient> CreateClientAsync(WebApplicationFactory<Program> api)
    {
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", await factory.Tokens.WithoutMemberLinkAsync());
        return client;
    }

    /// <summary>Counters of other tests (same account, same Redis) must not count here.</summary>
    private static string UniquePrefix() => $"test-{Guid.NewGuid():N}";
}
