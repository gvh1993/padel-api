using System.Net;
using AwesomeAssertions;
using Padel.Api.Features.Bookings;
using Padel.Api.Features.Courts;
using Padel.Api.Features.Members;
using Padel.Domain.Bookings;
using Padel.IntegrationTests.Clients;
using Padel.IntegrationTests.Infrastructure;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class BookingsApiTests(PadelApiFactory factory) : IntegrationTestBase(factory)
{
    private static CreateBookingRequest NewBooking(Guid memberId, Guid courtId, DateTimeOffset start, int minutes = 60) => new()
    {
        MemberId = memberId,
        CourtId = courtId,
        Start = start,
        End = start.AddMinutes(minutes),
    };

    [Test]
    public async Task Given_MemberToken_CreateBooking_Should_Return201AndAppearInCourtSchedule()
    {
        var member = await RegisterMemberAsync();
        var court = await CreateCourtAsync();
        var start = FutureSlot();

        var response = await As(member).Bookings.Create(NewBooking(member.Id, court.Id, start));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var booking = response.Value();
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.Start.Should().Be(start);
        booking.End.Should().Be(start.AddHours(1));

        var schedule = (await As(member).Courts.ListBookings(court.Id)).Value();
        schedule.Items.Select(b => b.Id).Should().BeEquivalentTo(new[] { booking.Id });
    }

    [Test]
    public async Task Given_LocalTimeOffset_CreateBooking_Should_StoreUtc()
    {
        var member = await RegisterMemberAsync();
        var court = await CreateCourtAsync();
        var startUtc = FutureSlot();
        var startAmsterdam = startUtc.ToOffset(TimeSpan.FromHours(1));

        var booking = (await As(member).Bookings.Create(NewBooking(member.Id, court.Id, startAmsterdam))).Value();

        booking.Start.Offset.Should().Be(TimeSpan.Zero);
        booking.Start.Should().Be(startUtc);
    }

    [Test]
    public async Task Given_OverlappingSlot_CreateBooking_Should_Return409()
    {
        var member = await RegisterMemberAsync();
        var court = await CreateCourtAsync();
        var start = FutureSlot();
        await BookAsync(member, court, start);

        var response = await As(member).Bookings.Create(NewBooking(member.Id, court.Id, start.AddMinutes(30)));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadProblemAsync()).Detail.Should().Contain("already booked");
    }

    [Test]
    public async Task Given_ConcurrentRequestsForTheSameSlot_Should_CreateExactlyOneBooking()
    {
        var court = await CreateCourtAsync();
        var members = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => RegisterMemberAsync()));
        var start = FutureSlot();

        var responses = await Task.WhenAll(members.Select(member =>
            As(member).Bookings.Create(NewBooking(member.Id, court.Id, start))));

        var statusCodes = responses.Select(r => r.StatusCode).ToList();
        statusCodes.Count(s => s == HttpStatusCode.Created).Should().Be(1);
        statusCodes.Count(s => s == HttpStatusCode.Conflict).Should().Be(7);

        var schedule = (await Admin.Courts.ListBookings(court.Id)).Value();
        schedule.TotalCount.Should().Be(1);
    }

    [Test]
    public async Task Given_SuspendedMember_CreateBooking_Should_Return422()
    {
        var member = await RegisterMemberAsync();
        var court = await CreateCourtAsync();
        await Admin.Members.Suspend(member.Id, new SuspendMemberRequest());

        var response = await As(member).Bookings.Create(NewBooking(member.Id, court.Id, FutureSlot()));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.ReadProblemAsync()).Detail.Should().Contain("suspended");
    }

    [Test]
    public async Task Given_CourtInMaintenance_CreateBooking_Should_Return422()
    {
        var member = await RegisterMemberAsync();
        var court = await CreateCourtAsync();
        await Admin.Courts.SetMaintenance(court.Id, new SetCourtMaintenanceRequest { UnderMaintenance = true });

        var response = await As(member).Bookings.Create(NewBooking(member.Id, court.Id, FutureSlot()));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task Given_SlotInThePast_CreateBooking_Should_Return422()
    {
        var member = await RegisterMemberAsync();
        var court = await CreateCourtAsync();

        var response = await As(member).Bookings.Create(NewBooking(member.Id, court.Id, FutureSlot(daysAhead: -2)));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task Given_UnknownCourt_CreateBooking_Should_Return404()
    {
        var member = await RegisterMemberAsync();

        var response = await As(member).Bookings.Create(NewBooking(member.Id, Guid.NewGuid(), FutureSlot()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Given_TooShortSlot_CreateBooking_Should_Return400()
    {
        var member = await RegisterMemberAsync();
        var court = await CreateCourtAsync();

        var response = await As(member).Bookings.Create(NewBooking(member.Id, court.Id, FutureSlot(), minutes: 15));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadProblemAsync()).Errors.Keys.Should().Contain("end");
    }

    [Test]
    public async Task Given_BookingForAnotherMember_CreateBooking_Should_Return403()
    {
        var member = await RegisterMemberAsync();
        var other = await RegisterMemberAsync();
        var court = await CreateCourtAsync();

        var response = await As(member).Bookings.Create(NewBooking(other.Id, court.Id, FutureSlot()));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Given_OwnBooking_Cancel_Should_StoreReasonAndFreeTheSlot()
    {
        var member = await RegisterMemberAsync();
        var court = await CreateCourtAsync();
        var start = FutureSlot();
        var booking = await BookAsync(member, court, start);

        var cancelled = (await As(member).Bookings.Cancel(booking.Id, new CancelBookingRequest { Reason = "injury" })).Value();
        var rebooked = await As(member).Bookings.Create(NewBooking(member.Id, court.Id, start));

        cancelled.Status.Should().Be(BookingStatus.Cancelled);
        cancelled.CancellationReason.Should().Be("injury");
        rebooked.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Test]
    public async Task Given_BookingOfAnotherMember_Cancel_Should_Return403()
    {
        var owner = await RegisterMemberAsync();
        var other = await RegisterMemberAsync();
        var booking = await BookAsync(owner, await CreateCourtAsync(), FutureSlot());

        var response = await As(other).Bookings.Cancel(booking.Id, new CancelBookingRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Given_CancelledBooking_Cancel_Should_Return422()
    {
        var member = await RegisterMemberAsync();
        var booking = await BookAsync(member, await CreateCourtAsync(), FutureSlot());
        await As(member).Bookings.Cancel(booking.Id, new CancelBookingRequest());

        var response = await As(member).Bookings.Cancel(booking.Id, new CancelBookingRequest());

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task Given_FutureBooking_Complete_Should_Return422()
    {
        var member = await RegisterMemberAsync();
        var booking = await BookAsync(member, await CreateCourtAsync(), FutureSlot());

        var response = await Admin.Bookings.Complete(booking.Id);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task Given_MemberToken_MarkNoShow_Should_Return403()
    {
        var member = await RegisterMemberAsync();
        var booking = await BookAsync(member, await CreateCourtAsync(), FutureSlot());

        var response = await As(member).Bookings.MarkNoShow(booking.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Given_MemberToken_ListBookings_Should_OnlyReturnOwnBookings()
    {
        var court = await CreateCourtAsync();
        var member = await RegisterMemberAsync();
        var other = await RegisterMemberAsync();
        var own = await BookAsync(member, court, FutureSlot(hour: 9));
        await BookAsync(other, court, FutureSlot(hour: 12));

        var page = (await As(member).Bookings.List()).Value();

        page.Items.Select(b => b.Id).Should().BeEquivalentTo(new[] { own.Id });
    }

    [Test]
    public async Task Given_MemberFilteringOnAnotherMember_ListBookings_Should_Return403()
    {
        var member = await RegisterMemberAsync();
        var other = await RegisterMemberAsync();

        var response = await As(member).Bookings.List(new BookingListParameters { MemberId = other.Id });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Given_PeriodStatusAndSort_ListCourtBookings_Should_ReturnMatchingBookings()
    {
        var member = await RegisterMemberAsync();
        var court = await CreateCourtAsync();
        var day = FutureSlot(daysAhead: 10, hour: 0);
        var morning = await BookAsync(member, court, day.AddHours(9));
        var evening = await BookAsync(member, court, day.AddHours(19));
        var cancelled = await BookAsync(member, court, day.AddHours(12));
        await As(member).Bookings.Cancel(cancelled.Id, new CancelBookingRequest());
        await BookAsync(member, court, day.AddDays(1).AddHours(9));

        var page = (await As(member).Courts.ListBookings(court.Id, new BookingListParameters
        {
            From = day,
            To = day.AddDays(1),
            Status = BookingStatus.Confirmed,
            Sort = "-start",
        })).Value();

        page.Items.Select(b => b.Id).Should().Equal(new[] { evening.Id, morning.Id });
    }

    [Test]
    public async Task Given_UnknownCourt_ListCourtBookings_Should_Return404()
    {
        var response = await Admin.Courts.ListBookings(Guid.NewGuid());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Given_RetryWithTheSameIdempotencyKey_CreateBooking_Should_ReturnTheSameBooking()
    {
        var member = await RegisterMemberAsync();
        var court = await CreateCourtAsync();
        var request = NewBooking(member.Id, court.Id, FutureSlot());
        var key = Guid.NewGuid().ToString();

        var first = await As(member).Bookings.Create(request, key);
        var retry = await As(member).Bookings.Create(request, key);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        retry.StatusCode.Should().Be(HttpStatusCode.Created);
        retry.Value().Id.Should().Be(first.Value().Id);
    }

    [Test]
    public async Task Given_MemberBookedOnAnotherCourtAtTheSameTime_CreateBooking_Should_Return409()
    {
        var member = await RegisterMemberAsync();
        var start = FutureSlot();
        await BookAsync(member, await CreateCourtAsync(), start);

        var response = await As(member).Bookings.Create(NewBooking(member.Id, (await CreateCourtAsync()).Id, start));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadProblemAsync()).ErrorCode.Should().Be("Booking.MemberAlreadyBooked");
    }

    [Test]
    public async Task Given_SlotOutsideOpeningHours_CreateBooking_Should_Return422()
    {
        var member = await RegisterMemberAsync();

        // 03:00 UTC is 04:00 or 05:00 in Amsterdam: before the club opens.
        var response = await As(member).Bookings.Create(NewBooking(member.Id, (await CreateCourtAsync()).Id, FutureSlot(hour: 3)));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.ReadProblemAsync()).ErrorCode.Should().Be("Booking.OutsideOpeningHours");
    }
}
