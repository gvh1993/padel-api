using Padel.Application.Common;
using Padel.Application.Features.Courts;
using Padel.Domain.Common;
using Padel.Domain.Courts;
using Padel.UnitTests.Fakes;
using TUnit.Assertions.Enums;

namespace Padel.UnitTests.Application.Courts;

public class CourtHandlerTests
{
    private readonly HandlerFixture _fixture = new();

    [Test]
    public async Task CreateCourt_Should_PersistAvailableCourtAndRaiseCourtCreated()
    {
        var handler = new CreateCourtCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        var result = await handler.Handle(new CreateCourtCommand { Name = "Court A", Surface = "glass" }, CancellationToken.None);

        await Assert.That(result.Status).IsEqualTo(CourtStatus.Available);
        await Assert.That(_fixture.Courts.Store).HasSingleItem();
        await Assert.That(_fixture.UnitOfWork.PublishedEvents.Single()).IsTypeOf<CourtCreated>();
    }

    [Test]
    public async Task RenameCourt_Should_ChangeTheName()
    {
        var court = _fixture.AddCourt();
        var handler = new RenameCourtCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        var result = await handler.Handle(new RenameCourtCommand(court.Id, "Centre Court"), CancellationToken.None);

        await Assert.That(result.Name).IsEqualTo("Centre Court");
        await Assert.That(_fixture.UnitOfWork.SaveCount).IsEqualTo(1);
    }

    [Test]
    public async Task SetCourtMaintenance_Should_PutCourtInMaintenance()
    {
        var court = _fixture.AddCourt();
        var handler = new SetCourtMaintenanceCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        var result = await handler.Handle(new SetCourtMaintenanceCommand(court.Id, true), CancellationToken.None);

        await Assert.That(result.Status).IsEqualTo(CourtStatus.Maintenance);
    }

    [Test]
    public async Task Given_RetiredCourt_SetCourtMaintenance_Should_ThrowDomainException()
    {
        var court = _fixture.AddCourt();
        court.Retire();
        var handler = new SetCourtMaintenanceCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(new SetCourtMaintenanceCommand(court.Id, true), CancellationToken.None));
        await Assert.That(_fixture.UnitOfWork.SaveCount).IsEqualTo(0);
    }

    [Test]
    public async Task RetireCourt_Should_RetireAndRaiseCourtRetired()
    {
        var court = _fixture.AddCourt();
        var handler = new RetireCourtCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        var result = await handler.Handle(new RetireCourtCommand(court.Id), CancellationToken.None);

        await Assert.That(result.Status).IsEqualTo(CourtStatus.Retired);
        await Assert.That(_fixture.UnitOfWork.PublishedEvents.Single()).IsTypeOf<CourtRetired>();
    }

    [Test]
    public async Task Given_UnknownCourt_RetireCourt_Should_ThrowNotFoundException()
    {
        var handler = new RetireCourtCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new RetireCourtCommand(Guid.NewGuid()), CancellationToken.None));
        await Assert.That(_fixture.UnitOfWork.SaveCount).IsEqualTo(0);
    }

    [Test]
    public async Task Given_UnknownCourt_GetCourt_Should_ThrowNotFoundException()
    {
        var handler = new GetCourtQueryHandler(_fixture.Courts);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetCourtQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Test]
    public async Task Given_SurfaceAndStatusFilters_ListCourts_Should_ReturnMatchesSortedByName()
    {
        _fixture.AddCourt("Court C", "glass");
        _fixture.AddCourt("Court A", "Glass");
        _fixture.AddCourt("Court B", "concrete");
        _fixture.AddCourt("Court D", "glass").SetMaintenance(true);
        var handler = new ListCourtsQueryHandler(_fixture.Courts);

        var result = await handler.Handle(
            new ListCourtsQuery { Surface = "GLASS", Status = CourtStatus.Available },
            CancellationToken.None);

        await Assert.That(result.Items.Select(c => c.Name)).IsEquivalentTo(["Court A", "Court C"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Given_SearchTerm_ListCourts_Should_MatchNamesCaseInsensitively()
    {
        _fixture.AddCourt("Centre Court");
        _fixture.AddCourt("Court 2");
        var handler = new ListCourtsQueryHandler(_fixture.Courts);

        var result = await handler.Handle(new ListCourtsQuery { Search = "centre" }, CancellationToken.None);

        await Assert.That(result.Items).HasSingleItem();
    }
}
