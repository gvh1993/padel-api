using AwesomeAssertions;
using Padel.Domain.Courts;
using Padel.UnitTests.Builders;

namespace Padel.UnitTests.Domain.Courts;

public class CourtTests
{
    [Test]
    public void Create_ShouldCreateAvailableCourtAndRaiseCourtCreated()
    {
        var court = Court.Create("Court A", "glass").ShouldSucceed();

        court.Status.Should().Be(CourtStatus.Available);
        court.CanBeBooked.Should().BeTrue();
        var created = (CourtCreated)court.DomainEvents.Single();
        created.CourtId.Should().Be(court.Id);
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public void Create_WhenBlankNameOrSurface_ShouldFail(string value)
    {
        Court.Create(value, "glass").ShouldFail();
        Court.Create("Court A", value).ShouldFail();
    }

    [Test]
    public void Create_WhenNameLongerThanMaxLength_ShouldFail()
    {
        Court.Create(new string('a', Court.MaxLength + 1), "glass").ShouldFail();
    }

    [Test]
    public void Rename_ShouldTrimAndReplaceTheName()
    {
        var court = CourtBuilder.New().Generate();

        court.Rename("  Centre Court ");

        court.Name.Should().Be("Centre Court");
    }

    [Test]
    public void SetMaintenance_ShouldToggleBetweenMaintenanceAndAvailable()
    {
        var court = CourtBuilder.New().Generate();

        court.SetMaintenance(true);
        court.Status.Should().Be(CourtStatus.Maintenance);
        court.CanBeBooked.Should().BeFalse();

        court.SetMaintenance(false);
        court.Status.Should().Be(CourtStatus.Available);
    }

    [Test]
    public void Retire_ShouldRetireCourtAndRaiseCourtRetired()
    {
        var court = CourtBuilder.New().Generate();

        court.Retire();

        court.Status.Should().Be(CourtStatus.Retired);
        court.DomainEvents.Single().Should().BeOfType<CourtRetired>();
    }

    [Test]
    public void Retire_GivenRetiredCourt_ShouldNotRaiseSecondEvent()
    {
        var court = CourtBuilder.New().Retired().Generate();

        court.Retire();

        court.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void SetMaintenance_GivenRetiredCourt_ShouldFail()
    {
        var court = CourtBuilder.New().Retired().Generate();

        court.SetMaintenance(true).ShouldFail();
    }
}
