using AwesomeAssertions;
using Padel.Domain.Bookings;
using Padel.Domain.Common;
using Padel.UnitTests.Builders;

namespace Padel.UnitTests.Domain.Bookings;

/// <summary>
/// The club's booking policy: opening hours in the club's time zone, half-hour slots, a limit per member
/// and a cancellation notice. The default club is in Amsterdam (UTC+1 in winter, UTC+2 in summer).
/// </summary>
public class BookingRulesTests
{
    private static readonly DateTimeOffset Now = TestData.Now;
    private static readonly BookingPolicy Policy = new();

    /// <summary>A slot on 26 November 2026, given in Amsterdam time (UTC+1).</summary>
    private static TimeSlot WinterSlot(int hour, int minute, int minutes = 60)
    {
        var start = new DateTimeOffset(2026, 11, 26, hour, minute, 0, TimeSpan.FromHours(1));
        return TimeSlot.Create(start, start.AddMinutes(minutes)).ShouldSucceed();
    }

    private static Result<Booking> Book(TimeSlot slot, int upcomingBookings = 0) =>
        Booking.Create(MemberBuilder.New().Generate(), CourtBuilder.New().Generate(), slot, Now, Policy, upcomingBookings);

    [Test]
    [Arguments(7, 0)]
    [Arguments(12, 30)]
    [Arguments(22, 0)]
    public void Create_WhenSlotWithinOpeningHours_ShouldSucceed(int hour, int minute)
    {
        Book(WinterSlot(hour, minute)).ShouldSucceed();
    }

    [Test]
    [Arguments(6, 30)]
    [Arguments(22, 30)]
    [Arguments(23, 30)]
    public void Create_WhenSlotOutsideOpeningHours_ShouldFail(int hour, int minute)
    {
        Book(WinterSlot(hour, minute)).ShouldFail("between 07:00 and 23:00").Is<BusinessRuleError>();
    }

    [Test]
    public void Create_WhenSummerTime_ShouldUseTheClubsLocalOpeningHours()
    {
        // 05:00 UTC is 07:00 in Amsterdam in summer (UTC+2), but 06:00 in winter.
        var summerOpening = new DateTimeOffset(2027, 6, 15, 5, 0, 0, TimeSpan.Zero);
        var winterBeforeOpening = new DateTimeOffset(2026, 12, 15, 5, 0, 0, TimeSpan.Zero);

        Book(TimeSlot.Create(summerOpening, summerOpening.AddHours(1)).ShouldSucceed()).ShouldSucceed();
        Book(TimeSlot.Create(winterBeforeOpening, winterBeforeOpening.AddHours(1)).ShouldSucceed())
            .ShouldFail().Is<BusinessRuleError>().Code.Should().Be("Booking.OutsideOpeningHours");
    }

    [Test]
    [Arguments(10, 15, 60)]
    [Arguments(10, 0, 45)]
    public void Create_WhenSlotNotOnTheHalfHour_ShouldFail(int hour, int minute, int minutes)
    {
        Book(WinterSlot(hour, minute, minutes)).ShouldFail("multiple of 30 minutes").Is<BusinessRuleError>();
    }

    [Test]
    public void Create_GivenMemberAtTheLimitOfUpcomingBookings_ShouldFail()
    {
        Book(WinterSlot(10, 0), upcomingBookings: Policy.MaxUpcomingBookingsPerMember)
            .ShouldFail("at most 3 upcoming bookings").Is<BusinessRuleError>();
    }

    [Test]
    public void Create_GivenMemberBelowTheLimit_ShouldSucceed()
    {
        Book(WinterSlot(10, 0), upcomingBookings: Policy.MaxUpcomingBookingsPerMember - 1).ShouldSucceed();
    }

    [Test]
    public void Create_ShouldStoreTheIdempotencyKey()
    {
        var slot = WinterSlot(10, 0);
        var court = CourtBuilder.New().Generate();

        var booking = Booking.Create(MemberBuilder.New().Generate(), court, slot, Now, Policy, 0, "key-1").ShouldSucceed();

        booking.IdempotencyKey.Should().Be("key-1");
        booking.IsFor(court.Id, slot).Should().BeTrue();
        booking.IsFor(court.Id, WinterSlot(11, 0)).Should().BeFalse();
    }

    [Test]
    public void Cancel_WhenEnoughNotice_ShouldSucceed()
    {
        var booking = Book(WinterSlot(10, 0)).ShouldSucceed();

        booking.Cancel(null, booking.Slot.Start.AddHours(-2), Policy.CancellationNotice).ShouldSucceed();
    }

    [Test]
    public void Cancel_WhenLessThanTheNotice_ShouldFail()
    {
        var booking = Book(WinterSlot(10, 0)).ShouldSucceed();

        booking.Cancel(null, booking.Slot.Start.AddMinutes(-90), Policy.CancellationNotice)
            .ShouldFail("up to 2 hours before the start").Is<BusinessRuleError>();
        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Test]
    public void Cancel_WhenNoNoticeRequired_ShouldSucceedUntilTheStart()
    {
        var booking = Book(WinterSlot(10, 0)).ShouldSucceed();

        booking.Cancel(null, booking.Slot.Start.AddMinutes(-1), TimeSpan.Zero).ShouldSucceed();
    }

    [Test]
    public void Cancel_GivenStartedBooking_ShouldFail()
    {
        var booking = Book(WinterSlot(10, 0)).ShouldSucceed();

        booking.Cancel(null, booking.Slot.Start, TimeSpan.Zero).ShouldFail("after it has started").Is<BusinessRuleError>();
    }

    [Test]
    public void Policy_WhenUnknownTimeZoneOrClosedDay_ShouldBeInvalid()
    {
        new BookingPolicy().IsValid().Should().BeTrue();
        new BookingPolicy { TimeZone = "Mars/Olympus" }.IsValid().Should().BeFalse();
        new BookingPolicy { OpensAt = new(23, 0), ClosesAt = new(7, 0) }.IsValid().Should().BeFalse();
        new BookingPolicy { SlotMinutes = 7 }.IsValid().Should().BeFalse();
    }
}
