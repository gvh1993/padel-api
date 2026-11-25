using AwesomeAssertions;
using Padel.Domain.Common;

namespace Padel.UnitTests.Domain.Common;

public class TimeSlotTests
{
    private static readonly DateTimeOffset Base = new(2026, 11, 25, 9, 0, 0, TimeSpan.Zero);

    [Test]
    public void Create_Should_ExposeStartEndAndDuration()
    {
        var slot = TimeSlot.Create(Base, Base.AddHours(1)).ShouldSucceed();

        slot.Start.Should().Be(Base);
        slot.End.Should().Be(Base.AddHours(1));
        slot.Duration.Should().Be(TimeSpan.FromHours(1));
    }

    [Test]
    public void Given_LocalOffset_Should_NormalizeToUtc()
    {
        var amsterdam = new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.FromHours(1));

        var slot = TimeSlot.Create(amsterdam, amsterdam.AddHours(1)).ShouldSucceed();

        slot.Start.Offset.Should().Be(TimeSpan.Zero);
        slot.Start.Should().Be(Base);
    }

    [Test]
    [Arguments(-30)]
    [Arguments(0)]
    [Arguments(29)]
    [Arguments(181)]
    public void Given_InvalidDurationInMinutes_Should_Fail(int minutes)
    {
        TimeSlot.Create(Base, Base.AddMinutes(minutes)).ShouldFail();
    }

    [Test]
    [Arguments(30)]
    [Arguments(180)]
    public void Given_BoundaryDurationInMinutes_Should_BeValid(int minutes)
    {
        var slot = TimeSlot.Create(Base, Base.AddMinutes(minutes)).ShouldSucceed();

        slot.Duration.Should().Be(TimeSpan.FromMinutes(minutes));
    }

    [Test]
    [Arguments(0, true)]
    [Arguments(30, true)]
    [Arguments(-30, true)]
    [Arguments(60, false)]
    [Arguments(-60, false)]
    public void Overlaps_Should_TreatSlotsAsHalfOpenIntervals(int offsetMinutes, bool expected)
    {
        var slot = TimeSlot.Create(Base, Base.AddHours(1)).ShouldSucceed();
        var other = TimeSlot.Create(Base.AddMinutes(offsetMinutes), Base.AddMinutes(offsetMinutes).AddHours(1)).ShouldSucceed();

        slot.Overlaps(other).Should().Be(expected);
    }
}
