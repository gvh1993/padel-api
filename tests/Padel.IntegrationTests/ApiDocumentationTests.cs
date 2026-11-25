using System.Net;
using System.Text.Json;
using Padel.Api.Features.Courts;
using Padel.IntegrationTests.Infrastructure;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class ApiDocumentationTests(PadelApiFactory factory) : IntegrationTestBase(factory)
{
    [Test]
    public async Task OpenApiDocument_Should_BeAnonymousAndDescribeKeycloakAuthAndProblems()
    {
        using var response = await Anonymous.Raw.Get("openapi/v1.json");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        await Assert.That(root.GetProperty("info").GetProperty("title").GetString()).IsEqualTo("Padel API");
        var keycloak = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Keycloak");
        await Assert.That(keycloak.GetProperty("type").GetString()).IsEqualTo("oauth2");
        var authorizationUrl = keycloak.GetProperty("flows").GetProperty("authorizationCode").GetProperty("authorizationUrl").GetString();
        await Assert.That(authorizationUrl).IsEqualTo($"{Tokens.Authority}/protocol/openid-connect/auth");

        var getCourt = root.GetProperty("paths").GetProperty("/api/courts/{id}").GetProperty("get");
        var content = getCourt.GetProperty("responses").GetProperty("200").GetProperty("content");
        await Assert.That(content.TryGetProperty("application/json", out _)).IsTrue();

        var notFound = getCourt.GetProperty("responses").GetProperty("404").GetProperty("content");
        await Assert.That(notFound.TryGetProperty("application/problem+json", out _)).IsTrue();
    }

    [Test]
    public async Task OpenApiDocument_Should_DocumentPagingSortingAndFilterParameters()
    {
        using var response = await Anonymous.Raw.Get("openapi/v1.json");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var parameters = document.RootElement
            .GetProperty("paths").GetProperty("/api/members").GetProperty("get").GetProperty("parameters")
            .EnumerateArray()
            .Select(parameter => parameter.GetProperty("name").GetString()!)
            .ToList();

        await Assert.That(parameters).IsEquivalentTo(["page", "pageSize", "sort", "status", "search"]);
    }

    [Test]
    public async Task Scalar_Should_BeServedAnonymously()
    {
        using var response = await Anonymous.Raw.Get("scalar", "text/html");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType!.MediaType).IsEqualTo("text/html");
    }
}
