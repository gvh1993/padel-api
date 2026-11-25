using System.Net;
using AwesomeAssertions;
using Padel.IntegrationTests.Infrastructure;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class RateLimitingTests(PadelApiFactory factory)
{
    [Test]
    public async Task ListCourts_WhenMoreRequestsThanTheLimit_ShouldReturn429ProblemWithRetryAfter()
    {
        // The same containers and database, but an API host with a limit of 2 requests per minute.
        await using var limited = factory.WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:PermitsPerMinute", "2"));
        var client = limited.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", await factory.Tokens.WithoutMemberLinkAsync());

        (await client.GetAsync("api/courts")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("api/courts")).StatusCode.Should().Be(HttpStatusCode.OK);
        var rejected = await client.GetAsync("api/courts");

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejected.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        rejected.Headers.RetryAfter.Should().NotBeNull();
        (await client.GetAsync("health/live")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
