using System.Net;
using System.Xml.Linq;
using Padel.IntegrationTests.Infrastructure;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class ContentNegotiationTests(PadelApiFactory factory) : IntegrationTestBase(factory)
{
    private const string Xml = "application/xml";
    private static readonly XNamespace Rfc9457 = "urn:ietf:rfc:7807";

    [Test]
    public async Task Given_NoAcceptPreference_GetCourt_Should_ReturnJson()
    {
        var court = await CreateCourtAsync();

        using var response = await Admin.Raw.Get($"api/courts/{court.Id}", "*/*");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType!.MediaType).IsEqualTo("application/json");
    }

    [Test]
    public async Task Given_AcceptXml_GetCourt_Should_ReturnCourtAsXml()
    {
        var court = await CreateCourtAsync();

        using var response = await Admin.Raw.Get($"api/courts/{court.Id}", Xml);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType!.MediaType).IsEqualTo(Xml);
        var root = XDocument.Parse(await response.Content.ReadAsStringAsync()).Root!;
        await Assert.That(root.Name.LocalName).IsEqualTo("CourtDto");
        await Assert.That(root.Element("Name")!.Value).IsEqualTo(court.Name);
        await Assert.That(root.Element("Status")!.Value).IsEqualTo("Available");
    }

    [Test]
    public async Task Given_AcceptXml_GetBooking_Should_SerializeDateTimeOffsets()
    {
        var member = await RegisterMemberAsync();
        var booking = await BookAsync(member, await CreateCourtAsync(), FutureSlot());

        using var response = await Admin.Raw.Get($"api/bookings/{booking.Id}", Xml);

        var root = XDocument.Parse(await response.Content.ReadAsStringAsync()).Root!;
        await Assert.That(DateTimeOffset.Parse(root.Element("Start")!.Value)).IsEqualTo(booking.Start);
    }

    [Test]
    public async Task Given_AcceptXml_ListCourts_Should_ReturnPagedResultAsXml()
    {
        await CreateCourtAsync();

        using var response = await Admin.Raw.Get("api/courts", Xml, new() { ["pageSize"] = "5" });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var root = XDocument.Parse(await response.Content.ReadAsStringAsync()).Root!;
        await Assert.That(root.Name.LocalName).IsEqualTo("PagedResultOfCourtDto");
        await Assert.That(root.Element("PageSize")!.Value).IsEqualTo("5");
        await Assert.That(root.Element("Items")!.Elements("CourtDto").Any()).IsTrue();
    }

    [Test]
    public async Task Given_AcceptXml_UnknownCourt_Should_ReturnProblemDetailsAsXml()
    {
        using var response = await Admin.Raw.Get($"api/courts/{Guid.NewGuid()}", Xml);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(response.Content.Headers.ContentType!.MediaType).IsEqualTo("application/problem+xml");
        var root = XDocument.Parse(await response.Content.ReadAsStringAsync()).Root!;
        await Assert.That(root.Name).IsEqualTo(Rfc9457 + "problem");
        await Assert.That(root.Element(Rfc9457 + "status")!.Value).IsEqualTo("404");
        await Assert.That(root.Element(Rfc9457 + "type")!.Value).IsEqualTo("https://tools.ietf.org/html/rfc9110#section-15.5.5");
        await Assert.That(root.Element(Rfc9457 + "traceId")).IsNotNull();
    }

    [Test]
    public async Task Given_AcceptXml_InvalidPaging_Should_ReturnValidationErrorsAsXml()
    {
        using var response = await Admin.Raw.Get("api/courts", Xml, new() { ["pageSize"] = "0" });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(response.Content.Headers.ContentType!.MediaType).IsEqualTo("application/problem+xml");
        var errors = XDocument.Parse(await response.Content.ReadAsStringAsync()).Root!.Element(Rfc9457 + "errors")!;
        await Assert.That(errors.Element(Rfc9457 + "pageSize")!.Elements(Rfc9457 + "i").Any()).IsTrue();
    }

    [Test]
    public async Task Given_XmlPreferredOverJson_GetCourt_Should_ReturnXml()
    {
        var court = await CreateCourtAsync();

        using var response = await Admin.Raw.Get($"api/courts/{court.Id}", "application/json;q=0.5, application/xml");

        await Assert.That(response.Content.Headers.ContentType!.MediaType).IsEqualTo(Xml);
    }

    [Test]
    public async Task Given_UnsupportedAccept_GetCourt_Should_Return406()
    {
        var court = await CreateCourtAsync();

        using var response = await Admin.Raw.Get($"api/courts/{court.Id}", "text/csv");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotAcceptable);
        await Assert.That(response.Content.Headers.ContentType!.MediaType).IsEqualTo("application/problem+json");
    }
}
