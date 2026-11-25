using System.Net;
using AwesomeAssertions;
using Padel.Api.Features.Courts;
using Padel.Api.Features.Members;
using Padel.Domain.Courts;
using Padel.IntegrationTests.Clients;
using Padel.IntegrationTests.Infrastructure;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class CourtsApiTests(PadelApiFactory factory) : IntegrationTestBase(factory)
{
    [Test]
    public async Task CreateCourt_Should_Return201WithAvailableCourt()
    {
        var name = UniqueName("Court");

        var response = await Admin.Courts.Create(new CreateCourtRequest { Name = name, Surface = "glass" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var court = response.Value();
        court.Name.Should().Be(name);
        court.Status.Should().Be(CourtStatus.Available);
        response.Headers!.Location!.AbsolutePath.Should().Be($"/api/courts/{court.Id}");
    }

    [Test]
    public async Task Given_BlankName_CreateCourt_Should_Return400()
    {
        var response = await Admin.Courts.Create(new CreateCourtRequest { Name = " ", Surface = "glass" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadProblemAsync()).Errors.Keys.Should().Contain("name");
    }

    [Test]
    public async Task RenameMaintenanceAndRetire_Should_PersistEveryChange()
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
    public async Task Given_RetiredCourt_SetMaintenance_Should_Return422Problem()
    {
        var court = await CreateCourtAsync();
        await Admin.Courts.Retire(court.Id);

        var response = await Admin.Courts.SetMaintenance(court.Id, new SetCourtMaintenanceRequest { UnderMaintenance = true });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.ReadProblemAsync()).Detail.Should().Contain("retired");
    }

    [Test]
    public async Task Given_MemberToken_ListCourts_Should_FilterAndSort()
    {
        var tag = Guid.NewGuid().ToString("N")[..10];
        await CreateCourtAsync($"B {tag}", "glass");
        await CreateCourtAsync($"A {tag}", "Glass");
        await CreateCourtAsync($"C {tag}", "concrete");
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
    public async Task Given_MemberToken_CreateCourt_Should_Return403()
    {
        var member = await RegisterMemberAsync();

        var response = await As(member).Courts.Create(new CreateCourtRequest { Name = UniqueName("Court"), Surface = "glass" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
