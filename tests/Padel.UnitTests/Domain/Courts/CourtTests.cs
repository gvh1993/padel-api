using Padel.Domain.Common;
using Padel.Domain.Courts;

namespace Padel.UnitTests.Domain.Courts;

public class CourtTests
{
    private static Court CreateCourt() => Court.Create("Court A", "glass").ShouldSucceed();

    [Test]
    public async Task Create_Should_CreateAvailableCourtAndRaiseCourtCreated()
    {
        var court = CreateCourt();

        await Assert.That(court.Status).IsEqualTo(CourtStatus.Available);
        await Assert.That(court.CanBeBooked).IsTrue();
        var created = (CourtCreated)court.DomainEvents.Single();
        await Assert.That(created.CourtId).IsEqualTo(court.Id);
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Given_BlankNameOrSurface_Should_Fail(string value)
    {
        await Court.Create(value, "glass").ShouldFail();
        await Court.Create("Court A", value).ShouldFail();
    }

    [Test]
    public async Task Given_NameLongerThanMaxLength_Should_Fail()
    {
        await Court.Create(new string('a', Court.MaxLength + 1), "glass").ShouldFail();
    }

    [Test]
    public async Task Rename_Should_TrimAndReplaceTheName()
    {
        var court = CreateCourt();

        court.Rename("  Centre Court ");

        await Assert.That(court.Name).IsEqualTo("Centre Court");
    }

    [Test]
    public async Task SetMaintenance_Should_ToggleBetweenMaintenanceAndAvailable()
    {
        var court = CreateCourt();

        court.SetMaintenance(true);
        await Assert.That(court.Status).IsEqualTo(CourtStatus.Maintenance);
        await Assert.That(court.CanBeBooked).IsFalse();

        court.SetMaintenance(false);
        await Assert.That(court.Status).IsEqualTo(CourtStatus.Available);
    }

    [Test]
    public async Task Retire_Should_RetireCourtAndRaiseCourtRetired()
    {
        var court = CreateCourt();
        court.ClearDomainEvents();

        court.Retire();

        await Assert.That(court.Status).IsEqualTo(CourtStatus.Retired);
        await Assert.That(court.DomainEvents.Single()).IsTypeOf<CourtRetired>();
    }

    [Test]
    public async Task Given_RetiredCourt_Retire_Should_NotRaiseSecondEvent()
    {
        var court = CreateCourt();
        court.Retire();
        court.ClearDomainEvents();

        court.Retire();

        await Assert.That(court.DomainEvents).IsEmpty();
    }

    [Test]
    public async Task Given_RetiredCourt_SetMaintenance_Should_Fail()
    {
        var court = CreateCourt();
        court.Retire();

        await court.SetMaintenance(true).ShouldFail();
    }
}
