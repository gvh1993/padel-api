using System.Net;
using Padel.Api.Features.Courts;
using Padel.Application.Features.Courts;
using Padel.Domain.Courts;
using Padel.IntegrationTests.Clients;
using Padel.IntegrationTests.Infrastructure;
using TUnit.Assertions.Enums;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class CourtsApiTests(PadelApiFactory factory) : IntegrationTestBase(factory)
{
    [Test]
    public async Task CreateCourt_Should_Return201WithAvailableCourt()
    {
        var name = UniqueName("Court");

        var response = await Admin.Courts.Create(new CreateCourtCommand { Name = name, Surface = "glass" });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var court = response.Value();
        await Assert.That(court.Name).IsEqualTo(name);
        await Assert.That(court.Status).IsEqualTo(CourtStatus.Available);
        await Assert.That(response.Headers!.Location!.AbsolutePath).IsEqualTo($"/api/courts/{court.Id}");
    }

    [Test]
    public async Task Given_BlankName_CreateCourt_Should_Return400()
    {
        var response = await Admin.Courts.Create(new CreateCourtCommand { Name = " ", Surface = "glass" });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That((await response.ReadProblemAsync()).Errors.Keys).Contains("name");
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

        await Assert.That(renamed.Name).IsEqualTo(newName);
        await Assert.That(maintenance.Status).IsEqualTo(CourtStatus.Maintenance);
        await Assert.That(retired.Status).IsEqualTo(CourtStatus.Retired);
        await Assert.That(reloaded).IsEqualTo(retired);
    }

    [Test]
    public async Task Given_RetiredCourt_SetMaintenance_Should_Return422Problem()
    {
        var court = await CreateCourtAsync();
        await Admin.Courts.Retire(court.Id);

        var response = await Admin.Courts.SetMaintenance(court.Id, new SetCourtMaintenanceRequest { UnderMaintenance = true });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
        await Assert.That((await response.ReadProblemAsync()).Detail).Contains("retired");
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

        await Assert.That(page.Items.Select(c => c.Name)).IsEquivalentTo([$"A {tag}", $"B {tag}"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Given_MemberToken_CreateCourt_Should_Return403()
    {
        var member = await RegisterMemberAsync();

        var response = await As(member).Courts.Create(new CreateCourtCommand { Name = UniqueName("Court"), Surface = "glass" });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }
}
