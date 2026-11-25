using AwesomeAssertions;
using Padel.Application.Features.Bookings;
using Padel.Domain.Bookings;
using Padel.Domain.Common;
using Padel.UnitTests.Fakes;

namespace Padel.UnitTests.Application.Bookings;

public class BookingHandlerTests
{
    private static readonly DateTimeOffset TomorrowMidnight = new(2026, 11, 26, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Tomorrow10 = TomorrowMidnight.AddHours(10);

    private readonly HandlerFixture _fixture = new();

    private CreateBookingCommandHandler CreateHandler =>
        new(_fixture.Bookings, _fixture.Members, _fixture.Courts, _fixture.UnitOfWork, _fixture.CurrentUser, _fixture.Clock, _fixture.PolicyOptions);

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

        var result = await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10), CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(BookingStatus.Confirmed);
        result.Start.Should().Be(Tomorrow10);
        result.CreatedOn.Should().Be(HandlerFixture.Now);
        _fixture.UnitOfWork.SaveCount.Should().Be(1);
        _fixture.UnitOfWork.PublishedEvents.Single().Should().BeOfType<BookingCreated>();
    }

    [Test]
    public async Task Given_OverlappingBooking_CreateBooking_Should_FailWithConflict()
    {
        var member = _fixture.AddMember();
        var court = _fixture.AddCourt();
        _fixture.AddBooking(member, court, Tomorrow10);

        var error = await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10.AddMinutes(30)), CancellationToken.None).ShouldFail().Is<ConflictError>();

        error.Description.Should().Contain("already booked");
        _fixture.Bookings.Store.Should().ContainSingle();
        _fixture.UnitOfWork.SaveCount.Should().Be(0);
    }

    [Test]
    public async Task Given_CancelledOverlappingBooking_CreateBooking_Should_Succeed()
    {
        var member = _fixture.AddMember();
        var court = _fixture.AddCourt();
        _fixture.AddBooking(member, court, Tomorrow10).Cancel(null, HandlerFixture.Now, TimeSpan.Zero);

        var result = await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10), CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Test]
    public async Task Given_AdjacentBooking_CreateBooking_Should_Succeed()
    {
        var member = _fixture.AddMember();
        var court = _fixture.AddCourt();
        _fixture.AddBooking(member, court, Tomorrow10);

        var result = await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10.AddHours(1)), CancellationToken.None).ShouldSucceed();

        result.Start.Should().Be(Tomorrow10.AddHours(1));
    }

    [Test]
    public async Task Given_UnknownMember_CreateBooking_Should_FailWithNotFound()
    {
        var court = _fixture.AddCourt();

        await CreateHandler.Handle(Command(Guid.NewGuid(), court.Id, Tomorrow10), CancellationToken.None).ShouldFail().Is<NotFoundError>();
    }

    [Test]
    public async Task Given_UnknownCourt_CreateBooking_Should_FailWithNotFound()
    {
        var member = _fixture.AddMember();

        await CreateHandler.Handle(Command(member.Id, Guid.NewGuid(), Tomorrow10), CancellationToken.None).ShouldFail().Is<NotFoundError>();
    }

    [Test]
    public async Task Given_SuspendedMember_CreateBooking_Should_Fail()
    {
        var member = _fixture.AddMember();
        member.Suspend();
        var court = _fixture.AddCourt();

        await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10), CancellationToken.None).ShouldFail().Is<BusinessRuleError>();
        _fixture.UnitOfWork.SaveCount.Should().Be(0);
    }

    [Test]
    public async Task Given_SlotInThePast_CreateBooking_Should_Fail()
    {
        var member = _fixture.AddMember();
        var court = _fixture.AddCourt();

        await CreateHandler.Handle(Command(member.Id, court.Id, HandlerFixture.Now.AddHours(-2)), CancellationToken.None).ShouldFail().Is<BusinessRuleError>();
    }

    [Test]
    public async Task Given_MemberBookingForThemselves_CreateBooking_Should_Succeed()
    {
        var member = _fixture.AddMember();
        var court = _fixture.AddCourt();
        _fixture.CurrentUser = FakeCurrentUser.ForMember(member.Id);

        var result = await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10), CancellationToken.None).ShouldSucceed();

        result.MemberId.Should().Be(member.Id);
    }

    [Test]
    public async Task Given_MemberBookingForSomeoneElse_CreateBooking_Should_FailWithForbidden()
    {
        var member = _fixture.AddMember();
        var court = _fixture.AddCourt();
        _fixture.CurrentUser = FakeCurrentUser.ForMember(Guid.NewGuid());

        await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10), CancellationToken.None).ShouldFail().Is<ForbiddenError>();
    }

    [Test]
    public async Task Given_OwnBooking_CancelBooking_Should_CancelWithReason()
    {
        var member = _fixture.AddMember();
        var booking = _fixture.AddBooking(member, _fixture.AddCourt(), Tomorrow10);
        _fixture.CurrentUser = FakeCurrentUser.ForMember(member.Id);
        var handler = new CancelBookingCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.CurrentUser, _fixture.Clock, _fixture.PolicyOptions);

        var result = await handler.Handle(new CancelBookingCommand(booking.Id, "no time"), CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(BookingStatus.Cancelled);
        result.CancellationReason.Should().Be("no time");
        var cancelled = (BookingCancelled)_fixture.UnitOfWork.PublishedEvents.Single();
        cancelled.Reason.Should().Be("no time");
    }

    [Test]
    public async Task Given_BookingOfAnotherMember_CancelBooking_Should_FailWithForbidden()
    {
        var booking = _fixture.AddBooking(_fixture.AddMember(), _fixture.AddCourt(), Tomorrow10);
        _fixture.CurrentUser = FakeCurrentUser.ForMember(Guid.NewGuid());
        var handler = new CancelBookingCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.CurrentUser, _fixture.Clock, _fixture.PolicyOptions);

        await handler.Handle(new CancelBookingCommand(booking.Id), CancellationToken.None).ShouldFail().Is<ForbiddenError>();
        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Test]
    public async Task Given_UnknownBooking_CancelBooking_Should_FailWithNotFound()
    {
        var handler = new CancelBookingCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.CurrentUser, _fixture.Clock, _fixture.PolicyOptions);

        await handler.Handle(new CancelBookingCommand(Guid.NewGuid()), CancellationToken.None).ShouldFail().Is<NotFoundError>();
    }

    [Test]
    public async Task Given_MemberWithinTheCancellationNotice_CancelBooking_Should_Fail()
    {
        var member = _fixture.AddMember();
        var booking = _fixture.AddBooking(member, _fixture.AddCourt(), Tomorrow10);
        _fixture.Clock.SetUtcNow(Tomorrow10.AddHours(-1));
        _fixture.CurrentUser = FakeCurrentUser.ForMember(member.Id);
        var handler = new CancelBookingCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.CurrentUser, _fixture.Clock, _fixture.PolicyOptions);

        var error = await handler.Handle(new CancelBookingCommand(booking.Id), CancellationToken.None).ShouldFail().Is<BusinessRuleError>();

        error.Code.Should().Be("Booking.CancellationTooLate");
        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Test]
    public async Task Given_AdminWithinTheCancellationNotice_CancelBooking_Should_Cancel()
    {
        var booking = _fixture.AddBooking(_fixture.AddMember(), _fixture.AddCourt(), Tomorrow10);
        _fixture.Clock.SetUtcNow(Tomorrow10.AddHours(-1));
        var handler = new CancelBookingCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.CurrentUser, _fixture.Clock, _fixture.PolicyOptions);

        var result = await handler.Handle(new CancelBookingCommand(booking.Id), CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Test]
    public async Task Given_MemberBookedElsewhereAtTheSameTime_CreateBooking_Should_FailWithConflict()
    {
        var member = _fixture.AddMember();
        _fixture.AddBooking(member, _fixture.AddCourt("Court A"), Tomorrow10);

        var error = await CreateHandler.Handle(Command(member.Id, _fixture.AddCourt("Court B").Id, Tomorrow10), CancellationToken.None)
            .ShouldFail().Is<ConflictError>();

        error.Code.Should().Be("Booking.MemberAlreadyBooked");
        _fixture.UnitOfWork.SaveCount.Should().Be(0);
    }

    [Test]
    public async Task Given_MemberAtTheLimitOfUpcomingBookings_CreateBooking_Should_Fail()
    {
        var member = _fixture.AddMember();
        var court = _fixture.AddCourt();
        for (var day = 1; day <= _fixture.Policy.MaxUpcomingBookingsPerMember; day++)
        {
            _fixture.AddBooking(member, court, Tomorrow10.AddDays(day));
        }

        var error = await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10), CancellationToken.None)
            .ShouldFail().Is<BusinessRuleError>();

        error.Code.Should().Be("Booking.TooManyUpcomingBookings");
    }

    [Test]
    public async Task Given_RetryWithTheSameIdempotencyKey_CreateBooking_Should_ReturnTheSameBooking()
    {
        var member = _fixture.AddMember();
        var court = _fixture.AddCourt();
        var command = Command(member.Id, court.Id, Tomorrow10) with { IdempotencyKey = "retry-1" };

        var first = await CreateHandler.Handle(command, CancellationToken.None).ShouldSucceed();
        var retry = await CreateHandler.Handle(command, CancellationToken.None).ShouldSucceed();

        retry.Id.Should().Be(first.Id);
        _fixture.Bookings.Store.Should().ContainSingle();
        _fixture.UnitOfWork.SaveCount.Should().Be(1);
    }

    [Test]
    public async Task Given_IdempotencyKeyReusedForAnotherSlot_CreateBooking_Should_FailWithConflict()
    {
        var member = _fixture.AddMember();
        var court = _fixture.AddCourt();
        await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10) with { IdempotencyKey = "retry-1" }, CancellationToken.None).ShouldSucceed();

        var error = await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10.AddHours(2)) with { IdempotencyKey = "retry-1" }, CancellationToken.None)
            .ShouldFail().Is<ConflictError>();

        error.Code.Should().Be("Booking.IdempotencyKeyReused");
    }

    [Test]
    public async Task Given_SlotHasStarted_CompleteBooking_Should_CompleteBooking()
    {
        var booking = _fixture.AddBooking(_fixture.AddMember(), _fixture.AddCourt(), Tomorrow10);
        _fixture.Clock.SetUtcNow(Tomorrow10.AddMinutes(5));
        var handler = new CompleteBookingCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.Clock);

        var result = await handler.Handle(new CompleteBookingCommand(booking.Id), CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(BookingStatus.Completed);
    }

    [Test]
    public async Task Given_SlotHasNotStarted_CompleteBooking_Should_Fail()
    {
        var booking = _fixture.AddBooking(_fixture.AddMember(), _fixture.AddCourt(), Tomorrow10);
        var handler = new CompleteBookingCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.Clock);

        await handler.Handle(new CompleteBookingCommand(booking.Id), CancellationToken.None).ShouldFail().Is<BusinessRuleError>();
    }

    [Test]
    public async Task Given_SlotHasStarted_MarkBookingNoShow_Should_MarkAsNoShow()
    {
        var booking = _fixture.AddBooking(_fixture.AddMember(), _fixture.AddCourt(), Tomorrow10);
        _fixture.Clock.SetUtcNow(Tomorrow10.AddHours(2));
        var handler = new MarkBookingNoShowCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.Clock);

        var result = await handler.Handle(new MarkBookingNoShowCommand(booking.Id), CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(BookingStatus.NoShow);
    }

    [Test]
    public async Task Given_BookingOfAnotherMember_GetBooking_Should_FailWithForbidden()
    {
        var booking = _fixture.AddBooking(_fixture.AddMember(), _fixture.AddCourt(), Tomorrow10);
        _fixture.CurrentUser = FakeCurrentUser.ForMember(Guid.NewGuid());
        var handler = new GetBookingQueryHandler(_fixture.Bookings, _fixture.CurrentUser);

        await handler.Handle(new GetBookingQuery(booking.Id), CancellationToken.None).ShouldFail().Is<ForbiddenError>();
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

        var result = await handler.Handle(new ListBookingsQuery(), CancellationToken.None).ShouldSucceed();

        result.Items.Should().ContainSingle();
        result.Items[0].MemberId.Should().Be(me.Id);
    }

    [Test]
    public async Task Given_MemberFilteringOnAnotherMember_ListBookings_Should_FailWithForbidden()
    {
        _fixture.CurrentUser = FakeCurrentUser.ForMember(Guid.NewGuid());
        var handler = new ListBookingsQueryHandler(_fixture.Bookings, _fixture.CurrentUser);

        await handler.Handle(new ListBookingsQuery { MemberId = Guid.NewGuid() }, CancellationToken.None).ShouldFail().Is<ForbiddenError>();
    }

    [Test]
    public async Task Given_UserWithoutMemberLink_ListBookings_Should_FailWithForbidden()
    {
        _fixture.CurrentUser = new FakeCurrentUser();
        var handler = new ListBookingsQueryHandler(_fixture.Bookings, _fixture.CurrentUser);

        await handler.Handle(new ListBookingsQuery(), CancellationToken.None).ShouldFail().Is<ForbiddenError>();
    }

    [Test]
    public async Task Given_AdminWithCourtStatusAndPeriodFilters_ListBookings_Should_ReturnMatchesNewestFirst()
    {
        var member = _fixture.AddMember();
        var court = _fixture.AddCourt("Court A");
        var otherCourt = _fixture.AddCourt("Court B");
        var early = _fixture.AddBooking(member, court, Tomorrow10);
        var late = _fixture.AddBooking(member, court, Tomorrow10.AddHours(3));
        _fixture.AddBooking(member, court, Tomorrow10.AddHours(5)).Cancel(null, HandlerFixture.Now, TimeSpan.Zero);
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
        }, CancellationToken.None).ShouldSucceed();

        result.Items.Select(b => b.Id).Should().Equal(new[] { late.Id, early.Id });
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

        var result = await handler.Handle(new ListCourtBookingsQuery { CourtId = court.Id }, CancellationToken.None).ShouldSucceed();

        result.Items.Select(b => b.Id).Should().Equal(new[] { earlier.Id, later.Id });
    }

    [Test]
    public async Task Given_UnknownCourt_ListCourtBookings_Should_FailWithNotFound()
    {
        var handler = new ListCourtBookingsQueryHandler(_fixture.Bookings, _fixture.Courts);

        await handler.Handle(new ListCourtBookingsQuery { CourtId = Guid.NewGuid() }, CancellationToken.None).ShouldFail().Is<NotFoundError>();
    }
}
