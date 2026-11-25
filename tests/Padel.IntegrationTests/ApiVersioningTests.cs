using System.Net;
using AwesomeAssertions;
using Padel.IntegrationTests.Infrastructure;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class ApiVersioningTests(PadelApiFactory factory) : IntegrationTestBase(factory)
{
    [Test]
    public async Task Request_ShouldReportTheSupportedVersions()
    {
        using var response = await Admin.Raw.Get("api/v1/courts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("api-supported-versions").Should().Equal("1");
    }

    [Test]
    public async Task Request_WhenUnsupportedVersion_ShouldReturn404Problem()
    {
        // The version is part of the path, so /api/v2/courts is a resource that does not exist.
        using var response = await Admin.Raw.Get("api/v2/courts");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Test]
    public async Task Request_WithoutVersion_ShouldReturn404()
    {
        using var response = await Admin.Raw.Get("api/courts");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
