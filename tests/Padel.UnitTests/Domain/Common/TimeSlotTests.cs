using Padel.Domain.Common;

namespace Padel.UnitTests.Domain.Common;

public class TimeSlotTests
{
    private static readonly DateTimeOffset Base = new(2026, 11, 25, 9, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task Create_Should_ExposeStartEndAndDuration()
    {
        var slot = TimeSlot.Create(Base, Base.AddHours(1)).ShouldSucceed();

        await Assert.That(slot.Start).IsEqualTo(Base);
        await Assert.That(slot.End).IsEqualTo(Base.AddHours(1));
        await Assert.That(slot.Duration).IsEqualTo(TimeSpan.FromHours(1));
    }

    [Test]
    public async Task Given_LocalOffset_Should_NormalizeToUtc()
    {
        var amsterdam = new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.FromHours(1));

        var slot = TimeSlot.Create(amsterdam, amsterdam.AddHours(1)).ShouldSucceed();

        await Assert.That(slot.Start.Offset).IsEqualTo(TimeSpan.Zero);
        await Assert.That(slot.Start).IsEqualTo(Base);
    }

    [Test]
    [Arguments(-30)]
    [Arguments(0)]
    [Arguments(29)]
    [Arguments(181)]
    public async Task Given_InvalidDurationInMinutes_Should_Fail(int minutes)
    {
        await TimeSlot.Create(Base, Base.AddMinutes(minutes)).ShouldFail();
    }

    [Test]
    [Arguments(30)]
    [Arguments(180)]
    public async Task Given_BoundaryDurationInMinutes_Should_BeValid(int minutes)
    {
        var slot = TimeSlot.Create(Base, Base.AddMinutes(minutes)).ShouldSucceed();

        await Assert.That(slot.Duration).IsEqualTo(TimeSpan.FromMinutes(minutes));
    }

    [Test]
    [Arguments(0, true)]
    [Arguments(30, true)]
    [Arguments(-30, true)]
    [Arguments(60, false)]
    [Arguments(-60, false)]
    public async Task Overlaps_Should_TreatSlotsAsHalfOpenIntervals(int offsetMinutes, bool expected)
    {
        var slot = TimeSlot.Create(Base, Base.AddHours(1)).ShouldSucceed();
        var other = TimeSlot.Create(Base.AddMinutes(offsetMinutes), Base.AddMinutes(offsetMinutes).AddHours(1)).ShouldSucceed();

        await Assert.That(slot.Overlaps(other)).IsEqualTo(expected);
    }
}
