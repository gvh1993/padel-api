using AwesomeAssertions;
using Padel.Application.Features.Bookings;
using Padel.Domain.Bookings;
using Padel.Domain.Common;
using Padel.UnitTests.Builders;
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
    public async Task CreateBooking_ShouldPersistConfirmedBookingAndRaiseBookingCreated()
    {
        var member = _fixture.Add(MemberBuilder.New());
        var court = _fixture.Add(CourtBuilder.New());

        var result = await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10), CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(BookingStatus.Confirmed);
        result.Start.Should().Be(Tomorrow10);
        result.CreatedOn.Should().Be(HandlerFixture.Now);
        _fixture.UnitOfWork.SaveCount.Should().Be(1);
        _fixture.UnitOfWork.PublishedEvents.Single().Should().BeOfType<BookingCreated>();
    }

    [Test]
    public async Task CreateBooking_GivenOverlappingBooking_ShouldFailWithConflict()
    {
        var member = _fixture.Add(MemberBuilder.New());
        var court = _fixture.Add(CourtBuilder.New());
        _fixture.Add(BookingBuilder.New().For(member).On(court).StartingAt(Tomorrow10));

        var error = await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10.AddMinutes(30)), CancellationToken.None).ShouldFail().Is<ConflictError>();

        error.Description.Should().Contain("already booked");
        _fixture.Bookings.Store.Should().ContainSingle();
        _fixture.UnitOfWork.SaveCount.Should().Be(0);
    }

    [Test]
    public async Task CreateBooking_GivenCancelledOverlappingBooking_ShouldSucceed()
    {
        var member = _fixture.Add(MemberBuilder.New());
        var court = _fixture.Add(CourtBuilder.New());
        _fixture.Add(BookingBuilder.New().For(member).On(court).StartingAt(Tomorrow10).Cancelled());

        var result = await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10), CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Test]
    public async Task CreateBooking_GivenAdjacentBooking_ShouldSucceed()
    {
        var member = _fixture.Add(MemberBuilder.New());
        var court = _fixture.Add(CourtBuilder.New());
        _fixture.Add(BookingBuilder.New().For(member).On(court).StartingAt(Tomorrow10));

        var result = await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10.AddHours(1)), CancellationToken.None).ShouldSucceed();

        result.Start.Should().Be(Tomorrow10.AddHours(1));
    }

    [Test]
    public async Task CreateBooking_WhenUnknownMember_ShouldFailWithNotFound()
    {
        var court = _fixture.Add(CourtBuilder.New());

        await CreateHandler.Handle(Command(Guid.NewGuid(), court.Id, Tomorrow10), CancellationToken.None).ShouldFail().Is<NotFoundError>();
    }

    [Test]
    public async Task CreateBooking_WhenUnknownCourt_ShouldFailWithNotFound()
    {
        var member = _fixture.Add(MemberBuilder.New());

        await CreateHandler.Handle(Command(member.Id, Guid.NewGuid(), Tomorrow10), CancellationToken.None).ShouldFail().Is<NotFoundError>();
    }

    [Test]
    public async Task CreateBooking_GivenSuspendedMember_ShouldFail()
    {
        var member = _fixture.Add(MemberBuilder.New().Suspended());
        var court = _fixture.Add(CourtBuilder.New());

        await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10), CancellationToken.None).ShouldFail().Is<BusinessRuleError>();
        _fixture.UnitOfWork.SaveCount.Should().Be(0);
    }

    [Test]
    public async Task CreateBooking_WhenSlotInThePast_ShouldFail()
    {
        var member = _fixture.Add(MemberBuilder.New());
        var court = _fixture.Add(CourtBuilder.New());

        await CreateHandler.Handle(Command(member.Id, court.Id, HandlerFixture.Now.AddHours(-2)), CancellationToken.None).ShouldFail().Is<BusinessRuleError>();
    }

    [Test]
    public async Task CreateBooking_GivenMemberBookingForThemselves_ShouldSucceed()
    {
        var member = _fixture.Add(MemberBuilder.New());
        var court = _fixture.Add(CourtBuilder.New());
        _fixture.CurrentUser = FakeCurrentUser.ForMember(member.Id);

        var result = await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10), CancellationToken.None).ShouldSucceed();

        result.MemberId.Should().Be(member.Id);
    }

    [Test]
    public async Task CreateBooking_GivenMemberBookingForSomeoneElse_ShouldFailWithForbidden()
    {
        var member = _fixture.Add(MemberBuilder.New());
        var court = _fixture.Add(CourtBuilder.New());
        _fixture.CurrentUser = FakeCurrentUser.ForMember(Guid.NewGuid());

        await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10), CancellationToken.None).ShouldFail().Is<ForbiddenError>();
    }

    [Test]
    public async Task CancelBooking_GivenOwnBooking_ShouldCancelWithReason()
    {
        var member = _fixture.Add(MemberBuilder.New());
        var booking = _fixture.Add(BookingBuilder.New().For(member).StartingAt(Tomorrow10));
        _fixture.CurrentUser = FakeCurrentUser.ForMember(member.Id);
        var handler = new CancelBookingCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.CurrentUser, _fixture.Clock, _fixture.PolicyOptions);

        var result = await handler.Handle(new CancelBookingCommand(booking.Id, "no time"), CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(BookingStatus.Cancelled);
        result.CancellationReason.Should().Be("no time");
        var cancelled = (BookingCancelled)_fixture.UnitOfWork.PublishedEvents.Single();
        cancelled.Reason.Should().Be("no time");
    }

    [Test]
    public async Task CancelBooking_GivenBookingOfAnotherMember_ShouldFailWithForbidden()
    {
        var booking = _fixture.Add(BookingBuilder.New().StartingAt(Tomorrow10));
        _fixture.CurrentUser = FakeCurrentUser.ForMember(Guid.NewGuid());
        var handler = new CancelBookingCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.CurrentUser, _fixture.Clock, _fixture.PolicyOptions);

        await handler.Handle(new CancelBookingCommand(booking.Id), CancellationToken.None).ShouldFail().Is<ForbiddenError>();
        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Test]
    public async Task CancelBooking_WhenUnknownBooking_ShouldFailWithNotFound()
    {
        var handler = new CancelBookingCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.CurrentUser, _fixture.Clock, _fixture.PolicyOptions);

        await handler.Handle(new CancelBookingCommand(Guid.NewGuid()), CancellationToken.None).ShouldFail().Is<NotFoundError>();
    }

    [Test]
    public async Task CancelBooking_GivenMemberWithinTheCancellationNotice_ShouldFail()
    {
        var member = _fixture.Add(MemberBuilder.New());
        var booking = _fixture.Add(BookingBuilder.New().For(member).StartingAt(Tomorrow10));
        _fixture.Clock.SetUtcNow(Tomorrow10.AddHours(-1));
        _fixture.CurrentUser = FakeCurrentUser.ForMember(member.Id);
        var handler = new CancelBookingCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.CurrentUser, _fixture.Clock, _fixture.PolicyOptions);

        var error = await handler.Handle(new CancelBookingCommand(booking.Id), CancellationToken.None).ShouldFail().Is<BusinessRuleError>();

        error.Code.Should().Be("Booking.CancellationTooLate");
        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Test]
    public async Task CancelBooking_GivenAdminWithinTheCancellationNotice_ShouldCancel()
    {
        var booking = _fixture.Add(BookingBuilder.New().StartingAt(Tomorrow10));
        _fixture.Clock.SetUtcNow(Tomorrow10.AddHours(-1));
        var handler = new CancelBookingCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.CurrentUser, _fixture.Clock, _fixture.PolicyOptions);

        var result = await handler.Handle(new CancelBookingCommand(booking.Id), CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Test]
    public async Task CreateBooking_GivenMemberBookedElsewhereAtTheSameTime_ShouldFailWithConflict()
    {
        var member = _fixture.Add(MemberBuilder.New());
        _fixture.Add(BookingBuilder.New().For(member).On(_fixture.Add(CourtBuilder.New().WithName("Court A"))).StartingAt(Tomorrow10));

        var error = await CreateHandler.Handle(Command(member.Id, _fixture.Add(CourtBuilder.New().WithName("Court B")).Id, Tomorrow10), CancellationToken.None)
            .ShouldFail().Is<ConflictError>();

        error.Code.Should().Be("Booking.MemberAlreadyBooked");
        _fixture.UnitOfWork.SaveCount.Should().Be(0);
    }

    [Test]
    public async Task CreateBooking_GivenMemberAtTheLimitOfUpcomingBookings_ShouldFail()
    {
        var member = _fixture.Add(MemberBuilder.New());
        var court = _fixture.Add(CourtBuilder.New());
        for (var day = 1; day <= _fixture.Policy.MaxUpcomingBookingsPerMember; day++)
        {
            _fixture.Add(BookingBuilder.New().For(member).On(court).StartingAt(Tomorrow10.AddDays(day)));
        }

        var error = await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10), CancellationToken.None)
            .ShouldFail().Is<BusinessRuleError>();

        error.Code.Should().Be("Booking.TooManyUpcomingBookings");
    }

    [Test]
    public async Task CreateBooking_WhenRetryWithTheSameIdempotencyKey_ShouldReturnTheSameBooking()
    {
        var member = _fixture.Add(MemberBuilder.New());
        var court = _fixture.Add(CourtBuilder.New());
        var command = Command(member.Id, court.Id, Tomorrow10) with { IdempotencyKey = "retry-1" };

        var first = await CreateHandler.Handle(command, CancellationToken.None).ShouldSucceed();
        var retry = await CreateHandler.Handle(command, CancellationToken.None).ShouldSucceed();

        retry.Id.Should().Be(first.Id);
        _fixture.Bookings.Store.Should().ContainSingle();
        _fixture.UnitOfWork.SaveCount.Should().Be(1);
    }

    [Test]
    public async Task CreateBooking_WhenIdempotencyKeyReusedForAnotherSlot_ShouldFailWithConflict()
    {
        var member = _fixture.Add(MemberBuilder.New());
        var court = _fixture.Add(CourtBuilder.New());
        await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10) with { IdempotencyKey = "retry-1" }, CancellationToken.None).ShouldSucceed();

        var error = await CreateHandler.Handle(Command(member.Id, court.Id, Tomorrow10.AddHours(2)) with { IdempotencyKey = "retry-1" }, CancellationToken.None)
            .ShouldFail().Is<ConflictError>();

        error.Code.Should().Be("Booking.IdempotencyKeyReused");
    }

    [Test]
    public async Task CompleteBooking_GivenSlotHasStarted_ShouldCompleteBooking()
    {
        var booking = _fixture.Add(BookingBuilder.New().StartingAt(Tomorrow10));
        _fixture.Clock.SetUtcNow(Tomorrow10.AddMinutes(5));
        var handler = new CompleteBookingCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.Clock);

        var result = await handler.Handle(new CompleteBookingCommand(booking.Id), CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(BookingStatus.Completed);
    }

    [Test]
    public async Task CompleteBooking_GivenSlotHasNotStarted_ShouldFail()
    {
        var booking = _fixture.Add(BookingBuilder.New().StartingAt(Tomorrow10));
        var handler = new CompleteBookingCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.Clock);

        await handler.Handle(new CompleteBookingCommand(booking.Id), CancellationToken.None).ShouldFail().Is<BusinessRuleError>();
    }

    [Test]
    public async Task MarkBookingNoShow_GivenSlotHasStarted_ShouldMarkAsNoShow()
    {
        var booking = _fixture.Add(BookingBuilder.New().StartingAt(Tomorrow10));
        _fixture.Clock.SetUtcNow(Tomorrow10.AddHours(2));
        var handler = new MarkBookingNoShowCommandHandler(_fixture.Bookings, _fixture.UnitOfWork, _fixture.Clock);

        var result = await handler.Handle(new MarkBookingNoShowCommand(booking.Id), CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(BookingStatus.NoShow);
    }

    [Test]
    public async Task GetBooking_GivenBookingOfAnotherMember_ShouldFailWithForbidden()
    {
        var booking = _fixture.Add(BookingBuilder.New().StartingAt(Tomorrow10));
        _fixture.CurrentUser = FakeCurrentUser.ForMember(Guid.NewGuid());
        var handler = new GetBookingQueryHandler(_fixture.Bookings, _fixture.CurrentUser);

        await handler.Handle(new GetBookingQuery(booking.Id), CancellationToken.None).ShouldFail().Is<ForbiddenError>();
    }

    [Test]
    public async Task ListBookings_GivenMember_ShouldOnlyReturnOwnBookings()
    {
        var court = _fixture.Add(CourtBuilder.New());
        var me = _fixture.Add(MemberBuilder.New().WithEmail("me@example.com"));
        var other = _fixture.Add(MemberBuilder.New().WithEmail("other@example.com"));
        _fixture.Add(BookingBuilder.New().For(me).On(court).StartingAt(Tomorrow10));
        _fixture.Add(BookingBuilder.New().For(other).On(court).StartingAt(Tomorrow10.AddHours(2)));
        _fixture.CurrentUser = FakeCurrentUser.ForMember(me.Id);
        var handler = new ListBookingsQueryHandler(_fixture.Bookings, _fixture.CurrentUser);

        var result = await handler.Handle(new ListBookingsQuery(), CancellationToken.None).ShouldSucceed();

        result.Items.Should().ContainSingle();
        result.Items[0].MemberId.Should().Be(me.Id);
    }

    [Test]
    public async Task ListBookings_WhenMemberFilteringOnAnotherMember_ShouldFailWithForbidden()
    {
        _fixture.CurrentUser = FakeCurrentUser.ForMember(Guid.NewGuid());
        var handler = new ListBookingsQueryHandler(_fixture.Bookings, _fixture.CurrentUser);

        await handler.Handle(new ListBookingsQuery { MemberId = Guid.NewGuid() }, CancellationToken.None).ShouldFail().Is<ForbiddenError>();
    }

    [Test]
    public async Task ListBookings_GivenUserWithoutMemberLink_ShouldFailWithForbidden()
    {
        _fixture.CurrentUser = new FakeCurrentUser();
        var handler = new ListBookingsQueryHandler(_fixture.Bookings, _fixture.CurrentUser);

        await handler.Handle(new ListBookingsQuery(), CancellationToken.None).ShouldFail().Is<ForbiddenError>();
    }

    [Test]
    public async Task ListBookings_GivenAdminWithCourtStatusAndPeriodFilters_ShouldReturnMatchesNewestFirst()
    {
        var member = _fixture.Add(MemberBuilder.New());
        var court = _fixture.Add(CourtBuilder.New().WithName("Court A"));
        var otherCourt = _fixture.Add(CourtBuilder.New().WithName("Court B"));
        var early = _fixture.Add(BookingBuilder.New().For(member).On(court).StartingAt(Tomorrow10));
        var late = _fixture.Add(BookingBuilder.New().For(member).On(court).StartingAt(Tomorrow10.AddHours(3)));
        _fixture.Add(BookingBuilder.New().For(member).On(court).StartingAt(Tomorrow10.AddHours(5)).Cancelled());
        _fixture.Add(BookingBuilder.New().For(member).On(otherCourt).StartingAt(Tomorrow10));
        _fixture.Add(BookingBuilder.New().For(member).On(court).StartingAt(Tomorrow10.AddDays(7)));
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
    public async Task ListCourtBookings_ShouldReturnBookingsOfThatCourtSortedByStart()
    {
        var member = _fixture.Add(MemberBuilder.New());
        var court = _fixture.Add(CourtBuilder.New().WithName("Court A"));
        var later = _fixture.Add(BookingBuilder.New().For(member).On(court).StartingAt(Tomorrow10.AddHours(2)));
        var earlier = _fixture.Add(BookingBuilder.New().For(member).On(court).StartingAt(Tomorrow10));
        _fixture.Add(BookingBuilder.New().For(member).On(_fixture.Add(CourtBuilder.New().WithName("Court B"))).StartingAt(Tomorrow10));
        var handler = new ListCourtBookingsQueryHandler(_fixture.Bookings, _fixture.Courts);

        var result = await handler.Handle(new ListCourtBookingsQuery { CourtId = court.Id }, CancellationToken.None).ShouldSucceed();

        result.Items.Select(b => b.Id).Should().Equal(new[] { earlier.Id, later.Id });
    }

    [Test]
    public async Task ListCourtBookings_WhenUnknownCourt_ShouldFailWithNotFound()
    {
        var handler = new ListCourtBookingsQueryHandler(_fixture.Bookings, _fixture.Courts);

        await handler.Handle(new ListCourtBookingsQuery { CourtId = Guid.NewGuid() }, CancellationToken.None).ShouldFail().Is<NotFoundError>();
    }
}
