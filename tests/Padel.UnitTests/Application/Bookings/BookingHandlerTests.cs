using Padel.Application.Features.Bookings;
using Padel.Application.Common;
using Padel.Domain.Bookings;
using Padel.Domain.Common;
using Padel.UnitTests.Fakes;
using TUnit.Assertions.Enums;

namespace Padel.UnitTests.Application.Bookings;

public class BookingHandlerTests
{
    private static readonly DateTimeOffset TomorrowMidnight = new(2026, 11, 26, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Tomorrow10 = TomorrowMidnight.AddHours(10);

    private readonly HandlerFixture _fixture = new();

    private CreateBookingCommandHandler CreateHandler =>
        new(_fixture.Bookings, _fixture.Members, _fixture.Courts, _fixture.UnitOfWork, _fixture.CurrentUser, _fixture.Clock);

    private static CreateBookingCommand Command(Guid memberId, Guid courtId, DateTimeOffset start) => new()
    {
        MemberId = memberId,
        CourtId = courtId,
        Start = start,
        End = start.AddHours(1),
    };

    [Test]
    public async Task CreateBooking_Should_PersistConfirmedBookingAndRaiseBookingCreated()
    {
        var member = _fixture.AddMember();
        var court = _fixture.AddCourt();

        var result = await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10), CancellationToken.None);

        await Assert.That(result.Status).IsEqualTo(BookingStatus.Confirmed);
        await Assert.That(result.Start).IsEqualTo(Tomorrow10);
        await Assert.That(result.CreatedOn).IsEqualTo(HandlerFixture.Now);
        await Assert.That(_fixture.UnitOfWork.SaveCount).IsEqualTo(1);
        await Assert.That(_fixture.UnitOfWork.PublishedEvents.Single()).IsTypeOf<BookingCreated>();
    }

    [Test]
    public async Task Given_OverlappingBooking_CreateBooking_Should_ThrowConflictException()
    {
        var member = _fixture.AddMember();
        var court = _fixture.AddCourt();
        _fixture.AddBooking(member, court, Tomorrow10);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10.AddMinutes(30)), CancellationToken.None));

        await Assert.That(exception!.Message).Contains("already booked");
        await Assert.That(_fixture.Bookings.Store).HasSingleItem();
        await Assert.That(_fixture.UnitOfWork.SaveCount).IsEqualTo(0);
    }

    [Test]
    public async Task Given_CancelledOverlappingBooking_CreateBooking_Should_Succeed()
    {
        var member = _fixture.AddMember();
        var court = _fixture.AddCourt();
        _fixture.AddBooking(member, court, Tomorrow10).Cancel();

        var result = await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10), CancellationToken.None);

        await Assert.That(result.Status).IsEqualTo(BookingStatus.Confirmed);
    }

    [Test]
    public async Task Given_AdjacentBooking_CreateBooking_Should_Succeed()
    {
        var member = _fixture.AddMember();
        var court = _fixture.AddCourt();
        _fixture.AddBooking(member, court, Tomorrow10);

        var result = await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10.AddHours(1)), CancellationToken.None);

        await Assert.That(result.Start).IsEqualTo(Tomorrow10.AddHours(1));
    }

    [Test]
    public async Task Given_UnknownMember_CreateBooking_Should_ThrowNotFoundException()
    {
        var court = _fixture.AddCourt();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler.Handle(Command(Guid.NewGuid(), court.Id, Tomorrow10), CancellationToken.None));
    }

    [Test]
    public async Task Given_UnknownCourt_CreateBooking_Should_ThrowNotFoundException()
    {
        var member = _fixture.AddMember();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler.Handle(Command(member.Id, Guid.NewGuid(), Tomorrow10), CancellationToken.None));
    }

    [Test]
    public async Task Given_SuspendedMember_CreateBooking_Should_ThrowDomainException()
    {
        var member = _fixture.AddMember();
        member.Suspend();
        var court = _fixture.AddCourt();

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10), CancellationToken.None));
        await Assert.That(_fixture.UnitOfWork.SaveCount).IsEqualTo(0);
    }

    [Test]
    public async Task Given_SlotInThePast_CreateBooking_Should_ThrowDomainException()
    {
        var member = _fixture.AddMember();
        var court = _fixture.AddCourt();

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler.Handle(Command(member.Id, court.Id, HandlerFixture.Now.AddHours(-2)), CancellationToken.None));
    }

    [Test]
    public async Task Given_MemberBookingForThemselves_CreateBooking_Should_Succeed()
    {
        var member = _fixture.AddMember();
        var court = _fixture.AddCourt();
        _fixture.CurrentUser = FakeCurrentUser.ForMember(member.Id);

        var result = await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10), CancellationToken.None);

        await Assert.That(result.MemberId).IsEqualTo(member.Id);
    }

    [Test]
    public async Task Given_MemberBookingForSomeoneElse_CreateBooking_Should_ThrowForbiddenException()
    {
        var member = _fixture.AddMember();
        var court = _fixture.AddCourt();
        _fixture.CurrentUser = FakeCurrentUser.ForMember(Guid.NewGuid());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10), CancellationToken.None));
    }

    [Test]
    public async Task Given_OwnBooking_CancelBooking_Should_CancelWithReason()
    {
        var member = _fixture.AddMember();
        var booking = _fixture.AddBooking(member, _fixture.AddCourt(), Tomorrow10);
        _fixture.CurrentUser = FakeCurrentUser.ForMember(member.Id);
        var handler = new CancelBookingCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.CurrentUser);

        var result = await handler.Handle(new CancelBookingCommand(booking.Id, "no time"), CancellationToken.None);

        await Assert.That(result.Status).IsEqualTo(BookingStatus.Cancelled);
        await Assert.That(result.CancellationReason).IsEqualTo("no time");
        var cancelled = (BookingCancelled)_fixture.UnitOfWork.PublishedEvents.Single();
        await Assert.That(cancelled.Reason).IsEqualTo("no time");
    }

    [Test]
    public async Task Given_BookingOfAnotherMember_CancelBooking_Should_ThrowForbiddenException()
    {
        var booking = _fixture.AddBooking(_fixture.AddMember(), _fixture.AddCourt(), Tomorrow10);
        _fixture.CurrentUser = FakeCurrentUser.ForMember(Guid.NewGuid());
        var handler = new CancelBookingCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.CurrentUser);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new CancelBookingCommand(booking.Id), CancellationToken.None));
        await Assert.That(booking.Status).IsEqualTo(BookingStatus.Confirmed);
    }

    [Test]
    public async Task Given_UnknownBooking_CancelBooking_Should_ThrowNotFoundException()
    {
        var handler = new CancelBookingCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.CurrentUser);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new CancelBookingCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Test]
    public async Task Given_SlotHasStarted_CompleteBooking_Should_CompleteBooking()
    {
        var booking = _fixture.AddBooking(_fixture.AddMember(), _fixture.AddCourt(), Tomorrow10);
        _fixture.Clock.SetUtcNow(Tomorrow10.AddMinutes(5));
        var handler = new CompleteBookingCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.Clock);

        var result = await handler.Handle(new CompleteBookingCommand(booking.Id), CancellationToken.None);

        await Assert.That(result.Status).IsEqualTo(BookingStatus.Completed);
    }

    [Test]
    public async Task Given_SlotHasNotStarted_CompleteBooking_Should_ThrowDomainException()
    {
        var booking = _fixture.AddBooking(_fixture.AddMember(), _fixture.AddCourt(), Tomorrow10);
        var handler = new CompleteBookingCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.Clock);

        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(new CompleteBookingCommand(booking.Id), CancellationToken.None));
    }

    [Test]
    public async Task Given_SlotHasStarted_MarkBookingNoShow_Should_MarkAsNoShow()
    {
        var booking = _fixture.AddBooking(_fixture.AddMember(), _fixture.AddCourt(), Tomorrow10);
        _fixture.Clock.SetUtcNow(Tomorrow10.AddHours(2));
        var handler = new MarkBookingNoShowCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.Clock);

        var result = await handler.Handle(new MarkBookingNoShowCommand(booking.Id), CancellationToken.None);

        await Assert.That(result.Status).IsEqualTo(BookingStatus.NoShow);
    }

    [Test]
    public async Task Given_BookingOfAnotherMember_GetBooking_Should_ThrowForbiddenException()
    {
        var booking = _fixture.AddBooking(_fixture.AddMember(), _fixture.AddCourt(), Tomorrow10);
        _fixture.CurrentUser = FakeCurrentUser.ForMember(Guid.NewGuid());
        var handler = new GetBookingQueryHandler(_fixture.Bookings, _fixture.CurrentUser);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new GetBookingQuery(booking.Id), CancellationToken.None));
    }

    [Test]
    public async Task Given_Member_ListBookings_Should_OnlyReturnOwnBookings()
    {
        var court = _fixture.AddCourt();
        var me = _fixture.AddMember("me@example.com");
        var other = _fixture.AddMember("other@example.com");
        _fixture.AddBooking(me, court, Tomorrow10);
        _fixture.AddBooking(other, court, Tomorrow10.AddHours(2));
        _fixture.CurrentUser = FakeCurrentUser.ForMember(me.Id);
        var handler = new ListBookingsQueryHandler(_fixture.Bookings, _fixture.CurrentUser);

        var result = await handler.Handle(new ListBookingsQuery(), CancellationToken.None);

        await Assert.That(result.Items).HasSingleItem();
        await Assert.That(result.Items[0].MemberId).IsEqualTo(me.Id);
    }

    [Test]
    public async Task Given_MemberFilteringOnAnotherMember_ListBookings_Should_ThrowForbiddenException()
    {
        _fixture.CurrentUser = FakeCurrentUser.ForMember(Guid.NewGuid());
        var handler = new ListBookingsQueryHandler(_fixture.Bookings, _fixture.CurrentUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new ListBookingsQuery { MemberId = Guid.NewGuid() }, CancellationToken.None));
    }

    [Test]
    public async Task Given_UserWithoutMemberLink_ListBookings_Should_ThrowForbiddenException()
    {
        _fixture.CurrentUser = new FakeCurrentUser();
        var handler = new ListBookingsQueryHandler(_fixture.Bookings, _fixture.CurrentUser);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new ListBookingsQuery(), CancellationToken.None));
    }

    [Test]
    public async Task Given_AdminWithCourtStatusAndPeriodFilters_ListBookings_Should_ReturnMatchesNewestFirst()
    {
        var member = _fixture.AddMember();
        var court = _fixture.AddCourt("Court A");
        var otherCourt = _fixture.AddCourt("Court B");
        var early = _fixture.AddBooking(member, court, Tomorrow10);
        var late = _fixture.AddBooking(member, court, Tomorrow10.AddHours(3));
        _fixture.AddBooking(member, court, Tomorrow10.AddHours(5)).Cancel();
        _fixture.AddBooking(member, otherCourt, Tomorrow10);
        _fixture.AddBooking(member, court, Tomorrow10.AddDays(7));
        var handler = new ListBookingsQueryHandler(_fixture.Bookings, _fixture.CurrentUser);

        var result = await handler.Handle(new ListBookingsQuery
        {
            CourtId = court.Id,
            Status = BookingStatus.Confirmed,
            From = TomorrowMidnight,
            To = TomorrowMidnight.AddDays(1),
            Sort = "-start",
        }, CancellationToken.None);

        await Assert.That(result.Items.Select(b => b.Id)).IsEquivalentTo([late.Id, early.Id], CollectionOrdering.Matching);
    }

    [Test]
    public async Task ListCourtBookings_Should_ReturnBookingsOfThatCourtSortedByStart()
    {
        var member = _fixture.AddMember();
        var court = _fixture.AddCourt("Court A");
        var later = _fixture.AddBooking(member, court, Tomorrow10.AddHours(2));
        var earlier = _fixture.AddBooking(member, court, Tomorrow10);
        _fixture.AddBooking(member, _fixture.AddCourt("Court B"), Tomorrow10);
        var handler = new ListCourtBookingsQueryHandler(_fixture.Bookings, _fixture.Courts);

        var result = await handler.Handle(new ListCourtBookingsQuery { CourtId = court.Id }, CancellationToken.None);

        await Assert.That(result.Items.Select(b => b.Id)).IsEquivalentTo([earlier.Id, later.Id], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Given_UnknownCourt_ListCourtBookings_Should_ThrowNotFoundException()
    {
        var handler = new ListCourtBookingsQueryHandler(_fixture.Bookings, _fixture.Courts);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ListCourtBookingsQuery { CourtId = Guid.NewGuid() }, CancellationToken.None));
    }
}
