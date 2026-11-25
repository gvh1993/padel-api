using AwesomeAssertions;
using NSubstitute;
using Padel.Application.Common;
using Padel.Application.Features.Courts;
using Padel.Domain.Common;
using Padel.Domain.Courts;
using Padel.UnitTests.Builders;
using Padel.UnitTests.Fakes;

namespace Padel.UnitTests.Application.Courts;

public class CourtHandlerTests
{
    private readonly HandlerFixture _fixture = new();

    [Test]
    public async Task CreateCourt_ShouldPersistAvailableCourtAndRaiseCourtCreated()
    {
        var handler = new CreateCourtCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        var result = await handler.Handle(new CreateCourtCommand { Name = "Court A", Surface = "glass" }, CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(CourtStatus.Available);
        _fixture.Courts.Store.Should().ContainSingle();
        _fixture.UnitOfWork.PublishedEvents.Single().Should().BeOfType<CourtCreated>();
    }

    [Test]
    public async Task RenameCourt_GivenSaveFails_ShouldReturnTheSaveError()
    {
        var court = _fixture.Add(CourtBuilder.New());
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(ApplicationErrors.ConcurrentChange);
        var handler = new RenameCourtCommandHandler(_fixture.Courts, unitOfWork);

        var error = await handler.Handle(new RenameCourtCommand(court.Id, "Centre"), CancellationToken.None).ShouldFail();

        error.Should().Be(ApplicationErrors.ConcurrentChange);
    }

    [Test]
    public async Task RenameCourt_ShouldChangeTheName()
    {
        var court = _fixture.Add(CourtBuilder.New());
        var handler = new RenameCourtCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        var result = await handler.Handle(new RenameCourtCommand(court.Id, "Centre Court"), CancellationToken.None).ShouldSucceed();

        result.Name.Should().Be("Centre Court");
        _fixture.UnitOfWork.SaveCount.Should().Be(1);
    }

    [Test]
    public async Task RenameCourt_WhenStaleVersion_ShouldFailWithPreconditionFailed()
    {
        var court = _fixture.Add(CourtBuilder.New().WithName("Court A"));
        var handler = new RenameCourtCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        var error = await handler.Handle(new RenameCourtCommand(court.Id, "Centre Court", ExpectedVersion: court.Version + 1), CancellationToken.None)
            .ShouldFail().Is<PreconditionFailedError>();

        error.Code.Should().Be("Court.Stale");
        court.Name.Should().Be("Court A");
        _fixture.UnitOfWork.SaveCount.Should().Be(0);
    }

    [Test]
    public async Task RenameCourt_WhenCurrentVersion_ShouldChangeTheName()
    {
        var court = _fixture.Add(CourtBuilder.New());
        var handler = new RenameCourtCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        var result = await handler.Handle(new RenameCourtCommand(court.Id, "Centre Court", ExpectedVersion: court.Version), CancellationToken.None).ShouldSucceed();

        result.Name.Should().Be("Centre Court");
    }

    [Test]
    public async Task SetCourtMaintenance_ShouldPutCourtInMaintenance()
    {
        var court = _fixture.Add(CourtBuilder.New());
        var handler = new SetCourtMaintenanceCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        var result = await handler.Handle(new SetCourtMaintenanceCommand(court.Id, true), CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(CourtStatus.Maintenance);
    }

    [Test]
    public async Task SetCourtMaintenance_GivenRetiredCourt_ShouldFail()
    {
        var court = _fixture.Add(CourtBuilder.New().Retired());
        var handler = new SetCourtMaintenanceCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        await handler.Handle(new SetCourtMaintenanceCommand(court.Id, true), CancellationToken.None).ShouldFail().Is<BusinessRuleError>();
        _fixture.UnitOfWork.SaveCount.Should().Be(0);
    }

    [Test]
    public async Task RetireCourt_ShouldRetireAndRaiseCourtRetired()
    {
        var court = _fixture.Add(CourtBuilder.New());
        var handler = new RetireCourtCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        var result = await handler.Handle(new RetireCourtCommand(court.Id), CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(CourtStatus.Retired);
        _fixture.UnitOfWork.PublishedEvents.Single().Should().BeOfType<CourtRetired>();
    }

    [Test]
    public async Task RetireCourt_WhenUnknownCourt_ShouldFailWithNotFound()
    {
        var handler = new RetireCourtCommandHandler(_fixture.Courts, _fixture.UnitOfWork);

        await handler.Handle(new RetireCourtCommand(Guid.NewGuid()), CancellationToken.None).ShouldFail().Is<NotFoundError>();
        _fixture.UnitOfWork.SaveCount.Should().Be(0);
    }

    [Test]
    public async Task GetCourt_WhenUnknownCourt_ShouldFailWithNotFound()
    {
        var handler = new GetCourtQueryHandler(_fixture.Courts);

        await handler.Handle(new GetCourtQuery(Guid.NewGuid()), CancellationToken.None).ShouldFail().Is<NotFoundError>();
    }

    [Test]
    public async Task ListCourts_WhenSurfaceAndStatusFilters_ShouldReturnMatchesSortedByName()
    {
        _fixture.Add(CourtBuilder.New().WithName("Court C").WithSurface("glass"));
        _fixture.Add(CourtBuilder.New().WithName("Court A").WithSurface("Glass"));
        _fixture.Add(CourtBuilder.New().WithName("Court B").WithSurface("concrete"));
        _fixture.Add(CourtBuilder.New().WithName("Court D").WithSurface("glass").InMaintenance());
        var handler = new ListCourtsQueryHandler(_fixture.Courts);

        var result = await handler.Handle(
            new ListCourtsQuery { Surface = "GLASS", Status = CourtStatus.Available },
            CancellationToken.None).ShouldSucceed();

        result.Items.Select(c => c.Name).Should().Equal(new[] { "Court A", "Court C" });
    }

    [Test]
    public async Task ListCourts_WhenSearchTerm_ShouldMatchNamesCaseInsensitively()
    {
        _fixture.Add(CourtBuilder.New().WithName("Centre Court"));
        _fixture.Add(CourtBuilder.New().WithName("Court 2"));
        var handler = new ListCourtsQueryHandler(_fixture.Courts);

        var result = await handler.Handle(new ListCourtsQuery { Search = "centre" }, CancellationToken.None).ShouldSucceed();

        result.Items.Should().ContainSingle();
    }
}
