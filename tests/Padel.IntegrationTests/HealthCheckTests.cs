using System.Net;
using AwesomeAssertions;
using Padel.IntegrationTests.Infrastructure;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class HealthCheckTests(PadelApiFactory factory) : IntegrationTestBase(factory)
{
    [Test]
    public async Task Liveness_Should_BeHealthyWithoutToken()
    {
        using var response = await Anonymous.Raw.Get("health/live", "text/plain");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }

    [Test]
    public async Task Given_DatabaseAvailable_Readiness_Should_BeHealthyWithoutToken()
    {
        using var response = await Anonymous.Raw.Get("health/ready", "text/plain");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }

    [Test]
    public async Task Given_NotDevelopment_DetailedHealth_Should_NotBeExposed()
    {
        using var response = await Anonymous.Raw.Get("health", "application/json");

        // Not mapped outside Development, so the fallback policy asks for a token.
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
