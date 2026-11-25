using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Padel.IntegrationTests.Infrastructure;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class ApiDocumentationTests(PadelApiFactory factory) : IntegrationTestBase(factory)
{
    [Test]
    public async Task OpenApiDocument_ShouldBeAnonymousAndDescribeKeycloakAuthAndProblems()
    {
        using var response = await Anonymous.Raw.Get("openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        root.GetProperty("info").GetProperty("title").GetString().Should().Be("Padel API");
        var keycloak = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Keycloak");
        keycloak.GetProperty("type").GetString().Should().Be("oauth2");
        var authorizationUrl = keycloak.GetProperty("flows").GetProperty("authorizationCode").GetProperty("authorizationUrl").GetString();
        authorizationUrl.Should().Be($"{Tokens.Authority}/protocol/openid-connect/auth");

        var getCourt = root.GetProperty("paths").GetProperty("/api/courts/{id}").GetProperty("get");
        var content = getCourt.GetProperty("responses").GetProperty("200").GetProperty("content");
        content.TryGetProperty("application/json", out _).Should().BeTrue();

        var notFound = getCourt.GetProperty("responses").GetProperty("404").GetProperty("content");
        notFound.TryGetProperty("application/problem+json", out _).Should().BeTrue();
    }

    [Test]
    public async Task OpenApiDocument_ShouldDocumentPagingSortingAndFilterParameters()
    {
        using var response = await Anonymous.Raw.Get("openapi/v1.json");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var parameters = document.RootElement
            .GetProperty("paths").GetProperty("/api/members").GetProperty("get").GetProperty("parameters")
            .EnumerateArray()
            .Select(parameter => parameter.GetProperty("name").GetString()!)
            .ToList();

        parameters.Should().BeEquivalentTo(new[] { "page", "pageSize", "sort", "status", "search" });
    }

    [Test]
    public async Task Scalar_ShouldBeServedAnonymously()
    {
        using var response = await Anonymous.Raw.Get("scalar", "text/html");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    }
}
