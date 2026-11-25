using AwesomeAssertions;
using Padel.Domain.Courts;

namespace Padel.UnitTests.Domain.Courts;

public class CourtTests
{
    private static Court CreateCourt() => Court.Create("Court A", "glass").ShouldSucceed();

    [Test]
    public void Create_Should_CreateAvailableCourtAndRaiseCourtCreated()
    {
        var court = CreateCourt();

        court.Status.Should().Be(CourtStatus.Available);
        court.CanBeBooked.Should().BeTrue();
        var created = (CourtCreated)court.DomainEvents.Single();
        created.CourtId.Should().Be(court.Id);
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public void Given_BlankNameOrSurface_Should_Fail(string value)
    {
        Court.Create(value, "glass").ShouldFail();
        Court.Create("Court A", value).ShouldFail();
    }

    [Test]
    public void Given_NameLongerThanMaxLength_Should_Fail()
    {
        Court.Create(new string('a', Court.MaxLength + 1), "glass").ShouldFail();
    }

    [Test]
    public void Rename_Should_TrimAndReplaceTheName()
    {
        var court = CreateCourt();

        court.Rename("  Centre Court ");

        court.Name.Should().Be("Centre Court");
    }

    [Test]
    public void SetMaintenance_Should_ToggleBetweenMaintenanceAndAvailable()
    {
        var court = CreateCourt();

        court.SetMaintenance(true);
        court.Status.Should().Be(CourtStatus.Maintenance);
        court.CanBeBooked.Should().BeFalse();

        court.SetMaintenance(false);
        court.Status.Should().Be(CourtStatus.Available);
    }

    [Test]
    public void Retire_Should_RetireCourtAndRaiseCourtRetired()
    {
        var court = CreateCourt();
        court.ClearDomainEvents();

        court.Retire();

        court.Status.Should().Be(CourtStatus.Retired);
        court.DomainEvents.Single().Should().BeOfType<CourtRetired>();
    }

    [Test]
    public void Given_RetiredCourt_Retire_Should_NotRaiseSecondEvent()
    {
        var court = CreateCourt();
        court.Retire();
        court.ClearDomainEvents();

        court.Retire();

        court.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void Given_RetiredCourt_SetMaintenance_Should_Fail()
    {
        var court = CreateCourt();
        court.Retire();

        court.SetMaintenance(true).ShouldFail();
    }
}
