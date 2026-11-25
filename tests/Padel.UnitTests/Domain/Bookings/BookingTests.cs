using AwesomeAssertions;
using Padel.Domain.Bookings;
using Padel.Domain.Common;
using Padel.Domain.Courts;
using Padel.Domain.Members;

namespace Padel.UnitTests.Domain.Bookings;

public class BookingTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 25, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Tomorrow = Now.AddDays(1);

    private static Member CreateMember() =>
        Member.Register(FullName.Create("Gert-Jan", "De Vries").ShouldSucceed(), Email.Create("gj@example.com").ShouldSucceed(), null, Now.AddDays(-1)).ShouldSucceed();

    private static Court CreateCourt() => Court.Create("Court A", "glass").ShouldSucceed();

    private static TimeSlot Slot(DateTimeOffset start) => TimeSlot.Create(start, start.AddHours(1)).ShouldSucceed();

    private static Booking CreateBooking(Court? court = null, DateTimeOffset? start = null) =>
        Booking.Create(CreateMember(), court ?? CreateCourt(), Slot(start ?? Tomorrow), Now).ShouldSucceed();

    [Test]
    public void Create_Should_CreateConfirmedBookingAndRaiseBookingCreated()
    {
        var member = CreateMember();
        var court = CreateCourt();

        var booking = Booking.Create(member, court, Slot(Tomorrow), Now).ShouldSucceed();

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
        var member = CreateMember();
        member.Suspend();

        Booking.Create(member, CreateCourt(), Slot(Tomorrow), Now).ShouldFail("suspended");
    }

    [Test]
    public void Given_CourtInMaintenance_Create_Should_Fail()
    {
        var court = CreateCourt();
        court.SetMaintenance(true);

        Booking.Create(CreateMember(), court, Slot(Tomorrow), Now).ShouldFail("not available");
    }

    [Test]
    public void Given_RetiredCourt_Create_Should_Fail()
    {
        var court = CreateCourt();
        court.Retire();

        Booking.Create(CreateMember(), court, Slot(Tomorrow), Now).ShouldFail().Is<BusinessRuleError>();
    }

    [Test]
    [Arguments(0)]
    [Arguments(-60)]
    public void Given_SlotThatDoesNotStartInTheFuture_Create_Should_Fail(int offsetMinutes)
    {
        Booking.Create(CreateMember(), CreateCourt(), Slot(Now.AddMinutes(offsetMinutes)), Now).ShouldFail("future");
    }

    [Test]
    public void Cancel_Should_StoreReasonAndRaiseBookingCancelled()
    {
        var booking = CreateBooking();
        booking.ClearDomainEvents();

        booking.Cancel(" no time ");

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.CancellationReason.Should().Be("no time");
        var cancelled = (BookingCancelled)booking.DomainEvents.Single();
        cancelled.Reason.Should().Be("no time");
    }

    [Test]
    public void Given_CancelledBooking_Cancel_Should_Fail()
    {
        var booking = CreateBooking();
        booking.Cancel();

        booking.Cancel().ShouldFail();
    }

    [Test]
    public void Given_CompletedBooking_Cancel_Should_Fail()
    {
        var booking = CreateBooking();
        booking.Complete(Tomorrow);

        booking.Cancel().ShouldFail();
    }

    [Test]
    public void Given_StartedBooking_Complete_Should_RaiseBookingCompleted()
    {
        var booking = CreateBooking();
        booking.ClearDomainEvents();

        booking.Complete(Tomorrow.AddMinutes(30));

        booking.Status.Should().Be(BookingStatus.Completed);
        booking.DomainEvents.Single().Should().BeOfType<BookingCompleted>();
    }

    [Test]
    public void Given_StartedBooking_MarkAsNoShow_Should_RaiseBookingNoShow()
    {
        var booking = CreateBooking();
        booking.ClearDomainEvents();

        booking.MarkAsNoShow(Tomorrow);

        booking.Status.Should().Be(BookingStatus.NoShow);
        booking.DomainEvents.Single().Should().BeOfType<BookingNoShow>();
    }

    [Test]
    public void Given_BookingThatHasNotStarted_Complete_Should_Fail()
    {
        var booking = CreateBooking();

        booking.Complete(Tomorrow.AddMinutes(-1)).ShouldFail("before it has started");
    }

    [Test]
    public void Given_NoShowBooking_Complete_Should_Fail()
    {
        var booking = CreateBooking();
        booking.MarkAsNoShow(Tomorrow);

        booking.Complete(Tomorrow).ShouldFail();
    }

    [Test]
    public void Given_CancelledBooking_MarkAsNoShow_Should_Fail()
    {
        var booking = CreateBooking();
        booking.Cancel();

        booking.MarkAsNoShow(Tomorrow).ShouldFail();
    }

    [Test]
    public void Overlaps_Should_OnlyMatchActiveBookingsOnTheSameCourt()
    {
        var court = CreateCourt();
        var booking = CreateBooking(court, Tomorrow);
        var overlapping = CreateBooking(court, Tomorrow.AddMinutes(30));
        var otherCourt = CreateBooking(start: Tomorrow.AddMinutes(30));
        var cancelled = CreateBooking(court, Tomorrow.AddMinutes(30));
        cancelled.Cancel();

        booking.Overlaps(overlapping).Should().BeTrue();
        booking.Overlaps(otherCourt).Should().BeFalse();
        booking.Overlaps(cancelled).Should().BeFalse();
    }
}
