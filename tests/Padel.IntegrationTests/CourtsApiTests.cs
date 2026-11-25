using System.Net;
using AwesomeAssertions;
using Padel.Api.Features.Courts;
using Padel.Domain.Courts;
using Padel.IntegrationTests.Builders;
using Padel.IntegrationTests.Clients;
using Padel.IntegrationTests.Infrastructure;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class CourtsApiTests(PadelApiFactory factory) : IntegrationTestBase(factory)
{
    [Test]
    public async Task CreateCourt_ShouldReturn201WithAvailableCourt()
    {
        var name = UniqueName("Court");

        var response = await Admin.Courts.Create(CreateCourtRequestBuilder.New().WithName(name).Generate());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var court = response.Value();
        court.Name.Should().Be(name);
        court.Status.Should().Be(CourtStatus.Available);
        response.Headers!.Location!.AbsolutePath.Should().Be($"/api/v1/courts/{court.Id}");
    }

    [Test]
    public async Task CreateCourt_WhenBlankName_ShouldReturn400()
    {
        var response = await Admin.Courts.Create(CreateCourtRequestBuilder.New().WithName(" ").Generate());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadProblemAsync()).Errors.Keys.Should().Contain("name");
    }

    [Test]
    public async Task RenameMaintenanceAndRetire_ShouldPersistEveryChange()
    {
        var court = await CreateCourtAsync();
        var newName = UniqueName("Centre");

        var renamed = (await Admin.Courts.Rename(court.Id, new RenameCourtRequest { Name = newName })).Value();
        var maintenance = (await Admin.Courts.SetMaintenance(court.Id, new SetCourtMaintenanceRequest { UnderMaintenance = true })).Value();
        var retired = (await Admin.Courts.Retire(court.Id)).Value();
        var reloaded = (await Admin.Courts.Get(court.Id)).Value();

        renamed.Name.Should().Be(newName);
        maintenance.Status.Should().Be(CourtStatus.Maintenance);
        retired.Status.Should().Be(CourtStatus.Retired);
        reloaded.Should().Be(new GetCourtResponse(retired.Id, retired.Name, retired.Surface, retired.Status));
    }

    [Test]
    public async Task SetMaintenance_GivenRetiredCourt_ShouldReturn422Problem()
    {
        var court = await CreateCourtAsync();
        await Admin.Courts.Retire(court.Id);

        var response = await Admin.Courts.SetMaintenance(court.Id, new SetCourtMaintenanceRequest { UnderMaintenance = true });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.ReadProblemAsync()).Detail.Should().Contain("retired");
    }

    [Test]
    public async Task ListCourts_GivenMemberToken_ShouldFilterAndSort()
    {
        var tag = Guid.NewGuid().ToString("N")[..10];
        await CreateCourtAsync(CreateCourtRequestBuilder.New().WithName($"B {tag}").WithSurface("glass"));
        await CreateCourtAsync(CreateCourtRequestBuilder.New().WithName($"A {tag}").WithSurface("Glass"));
        await CreateCourtAsync(CreateCourtRequestBuilder.New().WithName($"C {tag}").WithSurface("concrete"));
        var member = await RegisterMemberAsync();

        var page = (await As(member).Courts.List(new CourtListParameters
        {
            Search = tag,
            Surface = "GLASS",
            Status = CourtStatus.Available,
            Sort = "name",
        })).Value();

        page.Items.Select(c => c.Name).Should().Equal(new[] { $"A {tag}", $"B {tag}" });
    }

    [Test]
    public async Task CreateCourt_GivenMemberToken_ShouldReturn403()
    {
        var member = await RegisterMemberAsync();

        var response = await As(member).Courts.Create(CreateCourtRequestBuilder.New().Generate());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetCourt_ShouldReturnAnETagThatChangesOnUpdate()
    {
        var court = await CreateCourtAsync();

        var before = await Admin.Courts.Get(court.Id);
        var renamed = await Admin.Courts.Rename(court.Id, new RenameCourtRequest { Name = UniqueName("Renamed") }, before.Headers!.ETag!.ToString());

        before.Headers.ETag.Should().NotBeNull();
        renamed.StatusCode.Should().Be(HttpStatusCode.OK);
        renamed.Headers!.ETag!.ToString().Should().NotBe(before.Headers.ETag.ToString());
    }

    [Test]
    public async Task RenameCourt_WhenStaleETag_ShouldReturn412AndKeepTheNewerChange()
    {
        var court = await CreateCourtAsync();
        var staleETag = (await Admin.Courts.Get(court.Id)).Headers!.ETag!.ToString();
        var firstName = UniqueName("First");
        await Admin.Courts.Rename(court.Id, new RenameCourtRequest { Name = firstName }, staleETag);

        // A second client still has the old version.
        var response = await Admin.Courts.Rename(court.Id, new RenameCourtRequest { Name = UniqueName("Second") }, staleETag);

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        (await response.ReadProblemAsync()).ErrorCode.Should().Be("Court.Stale");
        (await Admin.Courts.Get(court.Id)).Value().Name.Should().Be(firstName);
    }

    [Test]
    public async Task RenameCourt_WhenNoIfMatch_ShouldApplyTheChange()
    {
        var court = await CreateCourtAsync();

        var response = await Admin.Courts.Rename(court.Id, new RenameCourtRequest { Name = UniqueName("Unconditional") });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
