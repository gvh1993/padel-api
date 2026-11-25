using AwesomeAssertions;
using Padel.Application.Common;
using Padel.Application.Features.Courts;
using Padel.Domain.Common;
using Padel.Domain.Courts;
using Padel.UnitTests.Fakes;

namespace Padel.UnitTests.Application.Courts;

public class CourtHandlerTests
{
    private readonly HandlerFixture _fixture = new();

    [Test]
    public async Task CreateCourt_Should_PersistAvailableCourtAndRaiseCourtCreated()
    {
        var handler = new CreateCourtCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        var result = await handler.Handle(new CreateCourtCommand { Name = "Court A", Surface = "glass" }, CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(CourtStatus.Available);
        _fixture.Courts.Store.Should().ContainSingle();
        _fixture.UnitOfWork.PublishedEvents.Single().Should().BeOfType<CourtCreated>();
    }

    [Test]
    public async Task Given_SaveFails_RenameCourt_Should_ReturnTheSaveError()
    {
        var court = _fixture.AddCourt();
        _fixture.UnitOfWork.FailWith = ApplicationErrors.ConcurrentChange;
        var handler = new RenameCourtCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        var error = await handler.Handle(new RenameCourtCommand(court.Id, "Centre"), CancellationToken.None).ShouldFail();

        error.Should().Be(ApplicationErrors.ConcurrentChange);
    }

    [Test]
    public async Task RenameCourt_Should_ChangeTheName()
    {
        var court = _fixture.AddCourt();
        var handler = new RenameCourtCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        var result = await handler.Handle(new RenameCourtCommand(court.Id, "Centre Court"), CancellationToken.None).ShouldSucceed();

        result.Name.Should().Be("Centre Court");
        _fixture.UnitOfWork.SaveCount.Should().Be(1);
    }

    [Test]
    public async Task Given_StaleVersion_RenameCourt_Should_FailWithPreconditionFailed()
    {
        var court = _fixture.AddCourt();
        var handler = new RenameCourtCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        var error = await handler.Handle(new RenameCourtCommand(court.Id, "Centre Court", ExpectedVersion: court.Version + 1), CancellationToken.None)
            .ShouldFail().Is<PreconditionFailedError>();

        error.Code.Should().Be("Court.Stale");
        court.Name.Should().Be("Court A");
        _fixture.UnitOfWork.SaveCount.Should().Be(0);
    }

    [Test]
    public async Task Given_CurrentVersion_RenameCourt_Should_ChangeTheName()
    {
        var court = _fixture.AddCourt();
        var handler = new RenameCourtCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        var result = await handler.Handle(new RenameCourtCommand(court.Id, "Centre Court", ExpectedVersion: court.Version), CancellationToken.None).ShouldSucceed();

        result.Name.Should().Be("Centre Court");
    }

    [Test]
    public async Task SetCourtMaintenance_Should_PutCourtInMaintenance()
    {
        var court = _fixture.AddCourt();
        var handler = new SetCourtMaintenanceCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        var result = await handler.Handle(new SetCourtMaintenanceCommand(court.Id, true), CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(CourtStatus.Maintenance);
    }

    [Test]
    public async Task Given_RetiredCourt_SetCourtMaintenance_Should_Fail()
    {
        var court = _fixture.AddCourt();
        court.Retire();
        var handler = new SetCourtMaintenanceCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        await handler.Handle(new SetCourtMaintenanceCommand(court.Id, true), CancellationToken.None).ShouldFail().Is<BusinessRuleError>();
        _fixture.UnitOfWork.SaveCount.Should().Be(0);
    }

    [Test]
    public async Task RetireCourt_Should_RetireAndRaiseCourtRetired()
    {
        var court = _fixture.AddCourt();
        var handler = new RetireCourtCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        var result = await handler.Handle(new RetireCourtCommand(court.Id), CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(CourtStatus.Retired);
        _fixture.UnitOfWork.PublishedEvents.Single().Should().BeOfType<CourtRetired>();
    }

    [Test]
    public async Task Given_UnknownCourt_RetireCourt_Should_FailWithNotFound()
    {
        var handler = new RetireCourtCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        await handler.Handle(new RetireCourtCommand(Guid.NewGuid()), CancellationToken.None).ShouldFail().Is<NotFoundError>();
        _fixture.UnitOfWork.SaveCount.Should().Be(0);
    }

    [Test]
    public async Task Given_UnknownCourt_GetCourt_Should_FailWithNotFound()
    {
        var handler = new GetCourtQueryHandler(_fixture.Courts);

        await handler.Handle(new GetCourtQuery(Guid.NewGuid()), CancellationToken.None).ShouldFail().Is<NotFoundError>();
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
            CancellationToken.None).ShouldSucceed();

        result.Items.Select(c => c.Name).Should().Equal(new[] { "Court A", "Court C" });
    }

    [Test]
    public async Task Given_SearchTerm_ListCourts_Should_MatchNamesCaseInsensitively()
    {
        _fixture.AddCourt("Centre Court");
        _fixture.AddCourt("Court 2");
        var handler = new ListCourtsQueryHandler(_fixture.Courts);

        var result = await handler.Handle(new ListCourtsQuery { Search = "centre" }, CancellationToken.None).ShouldSucceed();

        result.Items.Should().ContainSingle();
    }
}
