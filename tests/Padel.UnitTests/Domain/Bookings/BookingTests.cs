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
        new(new FullName("Gert-Jan", "De Vries"), new Email("gj@example.com"), null, Now.AddDays(-1));

    private static Court CreateCourt() => new("Court A", "glass");

    private static TimeSlot Slot(DateTimeOffset start) => new(start, start.AddHours(1));

    private static Booking CreateBooking(Court? court = null, DateTimeOffset? start = null) =>
        Booking.Create(CreateMember(), court ?? CreateCourt(), Slot(start ?? Tomorrow), Now);

    [Test]
    public async Task Create_Should_CreateConfirmedBookingAndRaiseBookingCreated()
    {
        var member = CreateMember();
        var court = CreateCourt();

        var booking = Booking.Create(member, court, Slot(Tomorrow), Now);

        await Assert.That(booking.Status).IsEqualTo(BookingStatus.Confirmed);
        await Assert.That(booking.MemberId).IsEqualTo(member.Id);
        await Assert.That(booking.CourtId).IsEqualTo(court.Id);
        await Assert.That(booking.CreatedOn).IsEqualTo(Now);
        var created = (BookingCreated)booking.DomainEvents.Single();
        await Assert.That(created.BookingId).IsEqualTo(booking.Id);
    }

    [Test]
    public async Task Given_SuspendedMember_Create_Should_ThrowDomainException()
    {
        var member = CreateMember();
        member.Suspend();

        await Assert.That(() => Booking.Create(member, CreateCourt(), Slot(Tomorrow), Now))
            .Throws<DomainException>()
            .WithMessageContaining("suspended");
    }

    [Test]
    public async Task Given_CourtInMaintenance_Create_Should_ThrowDomainException()
    {
        var court = CreateCourt();
        court.SetMaintenance(true);

        await Assert.That(() => Booking.Create(CreateMember(), court, Slot(Tomorrow), Now))
            .Throws<DomainException>()
            .WithMessageContaining("not available");
    }

    [Test]
    public async Task Given_RetiredCourt_Create_Should_ThrowDomainException()
    {
        var court = CreateCourt();
        court.Retire();

        await Assert.That(() => Booking.Create(CreateMember(), court, Slot(Tomorrow), Now)).Throws<DomainException>();
    }

    [Test]
    [Arguments(0)]
    [Arguments(-60)]
    public async Task Given_SlotThatDoesNotStartInTheFuture_Create_Should_ThrowDomainException(int offsetMinutes)
    {
        await Assert.That(() => Booking.Create(CreateMember(), CreateCourt(), Slot(Now.AddMinutes(offsetMinutes)), Now))
            .Throws<DomainException>()
            .WithMessageContaining("future");
    }

    [Test]
    public async Task Cancel_Should_StoreReasonAndRaiseBookingCancelled()
    {
        var booking = CreateBooking();
        booking.ClearDomainEvents();

        booking.Cancel(" no time ");

        await Assert.That(booking.Status).IsEqualTo(BookingStatus.Cancelled);
        await Assert.That(booking.CancellationReason).IsEqualTo("no time");
        var cancelled = (BookingCancelled)booking.DomainEvents.Single();
        await Assert.That(cancelled.Reason).IsEqualTo("no time");
    }

    [Test]
    public async Task Given_CancelledBooking_Cancel_Should_ThrowDomainException()
    {
        var booking = CreateBooking();
        booking.Cancel();

        await Assert.That(() => booking.Cancel()).Throws<DomainException>();
    }

    [Test]
    public async Task Given_CompletedBooking_Cancel_Should_ThrowDomainException()
    {
        var booking = CreateBooking();
        booking.Complete(Tomorrow);

        await Assert.That(() => booking.Cancel()).Throws<DomainException>();
    }

    [Test]
    public async Task Given_StartedBooking_Complete_Should_RaiseBookingCompleted()
    {
        var booking = CreateBooking();
        booking.ClearDomainEvents();

        booking.Complete(Tomorrow.AddMinutes(30));

        await Assert.That(booking.Status).IsEqualTo(BookingStatus.Completed);
        await Assert.That(booking.DomainEvents.Single()).IsTypeOf<BookingCompleted>();
    }

    [Test]
    public async Task Given_StartedBooking_MarkAsNoShow_Should_RaiseBookingNoShow()
    {
        var booking = CreateBooking();
        booking.ClearDomainEvents();

        booking.MarkAsNoShow(Tomorrow);

        await Assert.That(booking.Status).IsEqualTo(BookingStatus.NoShow);
        await Assert.That(booking.DomainEvents.Single()).IsTypeOf<BookingNoShow>();
    }

    [Test]
    public async Task Given_BookingThatHasNotStarted_Complete_Should_ThrowDomainException()
    {
        var booking = CreateBooking();

        await Assert.That(() => booking.Complete(Tomorrow.AddMinutes(-1)))
            .Throws<DomainException>()
            .WithMessageContaining("before it has started");
    }

    [Test]
    public async Task Given_NoShowBooking_Complete_Should_ThrowDomainException()
    {
        var booking = CreateBooking();
        booking.MarkAsNoShow(Tomorrow);

        await Assert.That(() => booking.Complete(Tomorrow)).Throws<DomainException>();
    }

    [Test]
    public async Task Given_CancelledBooking_MarkAsNoShow_Should_ThrowDomainException()
    {
        var booking = CreateBooking();
        booking.Cancel();

        await Assert.That(() => booking.MarkAsNoShow(Tomorrow)).Throws<DomainException>();
    }

    [Test]
    public async Task Overlaps_Should_OnlyMatchActiveBookingsOnTheSameCourt()
    {
        var court = CreateCourt();
        var booking = CreateBooking(court, Tomorrow);
        var overlapping = CreateBooking(court, Tomorrow.AddMinutes(30));
        var otherCourt = CreateBooking(start: Tomorrow.AddMinutes(30));
        var cancelled = CreateBooking(court, Tomorrow.AddMinutes(30));
        cancelled.Cancel();

        await Assert.That(booking.Overlaps(overlapping)).IsTrue();
        await Assert.That(booking.Overlaps(otherCourt)).IsFalse();
        await Assert.That(booking.Overlaps(cancelled)).IsFalse();
    }
}
