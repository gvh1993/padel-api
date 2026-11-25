using AwesomeAssertions;
using Padel.Domain.Bookings;
using Padel.Domain.Common;
using Padel.Domain.Courts;
using Padel.Domain.Members;
using Padel.UnitTests.Builders;

namespace Padel.UnitTests.Domain.Bookings;

public class BookingTests
{
    private static readonly DateTimeOffset Now = TestData.Now;
    private static readonly DateTimeOffset Tomorrow = TestData.Tomorrow10;
    private static readonly BookingPolicy Policy = new();

    private static TimeSlot Slot(DateTimeOffset start) => TimeSlot.Create(start, start.AddHours(1)).ShouldSucceed();

    private static Result<Booking> Book(Member member, Court court, DateTimeOffset start) =>
        Booking.Create(member, court, Slot(start), Now, Policy, upcomingBookingsOfMember: 0);

    [Test]
    public void Create_Should_CreateConfirmedBookingAndRaiseBookingCreated()
    {
        var member = MemberBuilder.New().Generate();
        var court = CourtBuilder.New().Generate();

        var booking = Book(member, court, Tomorrow).ShouldSucceed();

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.MemberId.Should().Be(member.Id);
        booking.CourtId.Should().Be(court.Id);
        booking.CreatedOn.Should().Be(Now);
        var created = (BookingCreated)booking.DomainEvents.Single();
        created.BookingId.Should().Be(booking.Id);
    }

    [Test]
    public void Given_SuspendedMember_Create_Should_Fail()
    {
        var member = MemberBuilder.New().Suspended().Generate();

        Book(member, CourtBuilder.New().Generate(), Tomorrow).ShouldFail("suspended");
    }

    [Test]
    public void Given_CourtInMaintenance_Create_Should_Fail()
    {
        var court = CourtBuilder.New().InMaintenance().Generate();

        Book(MemberBuilder.New().Generate(), court, Tomorrow).ShouldFail("not available");
    }

    [Test]
    public void Given_RetiredCourt_Create_Should_Fail()
    {
        var court = CourtBuilder.New().Retired().Generate();

        Book(MemberBuilder.New().Generate(), court, Tomorrow).ShouldFail().Is<BusinessRuleError>();
    }

    [Test]
    [Arguments(0)]
    [Arguments(-60)]
    public void Given_SlotThatDoesNotStartInTheFuture_Create_Should_Fail(int offsetMinutes)
    {
        Book(MemberBuilder.New().Generate(), CourtBuilder.New().Generate(), Now.AddMinutes(offsetMinutes)).ShouldFail("future");
    }

    [Test]
    public void Cancel_Should_StoreReasonAndRaiseBookingCancelled()
    {
        var booking = BookingBuilder.New().Generate();

        booking.Cancel(" no time ", Now, TimeSpan.Zero);

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.CancellationReason.Should().Be("no time");
        var cancelled = (BookingCancelled)booking.DomainEvents.Single();
        cancelled.Reason.Should().Be("no time");
    }

    [Test]
    public void Given_CancelledBooking_Cancel_Should_Fail()
    {
        var booking = BookingBuilder.New().Cancelled().Generate();

        booking.Cancel(null, Now, TimeSpan.Zero).ShouldFail();
    }

    [Test]
    public void Given_CompletedBooking_Cancel_Should_Fail()
    {
        var booking = BookingBuilder.New().Completed().Generate();

        booking.Cancel(null, Now, TimeSpan.Zero).ShouldFail();
    }

    [Test]
    public void Given_StartedBooking_Complete_Should_RaiseBookingCompleted()
    {
        var booking = BookingBuilder.New().StartingAt(Tomorrow).Generate();

        booking.Complete(Tomorrow.AddMinutes(30));

        booking.Status.Should().Be(BookingStatus.Completed);
        booking.DomainEvents.Single().Should().BeOfType<BookingCompleted>();
    }

    [Test]
    public void Given_StartedBooking_MarkAsNoShow_Should_RaiseBookingNoShow()
    {
        var booking = BookingBuilder.New().StartingAt(Tomorrow).Generate();

        booking.MarkAsNoShow(Tomorrow);

        booking.Status.Should().Be(BookingStatus.NoShow);
        booking.DomainEvents.Single().Should().BeOfType<BookingNoShow>();
    }

    [Test]
    public void Given_BookingThatHasNotStarted_Complete_Should_Fail()
    {
        var booking = BookingBuilder.New().StartingAt(Tomorrow).Generate();

        booking.Complete(Tomorrow.AddMinutes(-1)).ShouldFail("before it has started");
    }

    [Test]
    public void Given_NoShowBooking_Complete_Should_Fail()
    {
        var booking = BookingBuilder.New().NoShow().Generate();

        booking.Complete(Tomorrow).ShouldFail();
    }

    [Test]
    public void Given_CancelledBooking_MarkAsNoShow_Should_Fail()
    {
        var booking = BookingBuilder.New().Cancelled().Generate();

        booking.MarkAsNoShow(Tomorrow).ShouldFail();
    }

    [Test]
    public void Overlaps_Should_OnlyMatchActiveBookingsOnTheSameCourt()
    {
        var court = CourtBuilder.New().Generate();
        var booking = BookingBuilder.New().On(court).StartingAt(Tomorrow).Generate();
        var overlapping = BookingBuilder.New().On(court).StartingAt(Tomorrow.AddMinutes(30)).Generate();
        var otherCourt = BookingBuilder.New().StartingAt(Tomorrow.AddMinutes(30)).Generate();
        var cancelled = BookingBuilder.New().On(court).StartingAt(Tomorrow.AddMinutes(30)).Cancelled().Generate();

        booking.Overlaps(overlapping).Should().BeTrue();
        booking.Overlaps(otherCourt).Should().BeFalse();
        booking.Overlaps(cancelled).Should().BeFalse();
    }
}
