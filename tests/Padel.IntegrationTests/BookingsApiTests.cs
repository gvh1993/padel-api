using System.Net;
using Padel.Api.Features.Bookings;
using Padel.Api.Features.Courts;
using Padel.Api.Features.Members;
using Padel.Application.Features.Bookings;
using Padel.Domain.Bookings;
using Padel.IntegrationTests.Clients;
using Padel.IntegrationTests.Infrastructure;
using TUnit.Assertions.Enums;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class BookingsApiTests(PadelApiFactory factory) : IntegrationTestBase(factory)
{
    private static CreateBookingCommand Command(Guid memberId, Guid courtId, DateTimeOffset start, int minutes = 60) => new()
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

        var response = await As(member).Bookings.Create(Command(member.Id, court.Id, start));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var booking = response.Value();
        await Assert.That(booking.Status).IsEqualTo(BookingStatus.Confirmed);
        await Assert.That(booking.Start).IsEqualTo(start);
        await Assert.That(booking.End).IsEqualTo(start.AddHours(1));

        var schedule = (await As(member).Courts.ListBookings(court.Id)).Value();
        await Assert.That(schedule.Items.Select(b => b.Id)).IsEquivalentTo([booking.Id]);
    }

    [Test]
    public async Task Given_LocalTimeOffset_CreateBooking_Should_StoreUtc()
    {
        var member = await RegisterMemberAsync();
        var court = await CreateCourtAsync();
        var startUtc = FutureSlot();
        var startAmsterdam = startUtc.ToOffset(TimeSpan.FromHours(1));

        var booking = (await As(member).Bookings.Create(Command(member.Id, court.Id, startAmsterdam))).Value();

        await Assert.That(booking.Start.Offset).IsEqualTo(TimeSpan.Zero);
        await Assert.That(booking.Start).IsEqualTo(startUtc);
    }

    [Test]
    public async Task Given_OverlappingSlot_CreateBooking_Should_Return409()
    {
        var member = await RegisterMemberAsync();
        var court = await CreateCourtAsync();
        var start = FutureSlot();
        await BookAsync(member, court, start);

        var response = await As(member).Bookings.Create(Command(member.Id, court.Id, start.AddMinutes(30)));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        await Assert.That((await response.ReadProblemAsync()).Detail).Contains("already booked");
    }

    [Test]
    public async Task Given_ConcurrentRequestsForTheSameSlot_Should_CreateExactlyOneBooking()
    {
        var court = await CreateCourtAsync();
        var members = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => RegisterMemberAsync()));
        var start = FutureSlot();

        var responses = await Task.WhenAll(members.Select(member =>
            As(member).Bookings.Create(Command(member.Id, court.Id, start))));

        var statusCodes = responses.Select(r => r.StatusCode).ToList();
        await Assert.That(statusCodes.Count(s => s == HttpStatusCode.Created)).IsEqualTo(1);
        await Assert.That(statusCodes.Count(s => s == HttpStatusCode.Conflict)).IsEqualTo(7);

        var schedule = (await Admin.Courts.ListBookings(court.Id)).Value();
        await Assert.That(schedule.TotalCount).IsEqualTo(1);
    }

    [Test]
    public async Task Given_SuspendedMember_CreateBooking_Should_Return422()
    {
        var member = await RegisterMemberAsync();
        var court = await CreateCourtAsync();
        await Admin.Members.Suspend(member.Id, new SuspendMemberRequest());

        var response = await As(member).Bookings.Create(Command(member.Id, court.Id, FutureSlot()));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
        await Assert.That((await response.ReadProblemAsync()).Detail).Contains("suspended");
    }

    [Test]
    public async Task Given_CourtInMaintenance_CreateBooking_Should_Return422()
    {
        var member = await RegisterMemberAsync();
        var court = await CreateCourtAsync();
        await Admin.Courts.SetMaintenance(court.Id, new SetCourtMaintenanceRequest { UnderMaintenance = true });

        var response = await As(member).Bookings.Create(Command(member.Id, court.Id, FutureSlot()));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task Given_SlotInThePast_CreateBooking_Should_Return422()
    {
        var member = await RegisterMemberAsync();
        var court = await CreateCourtAsync();

        var response = await As(member).Bookings.Create(Command(member.Id, court.Id, FutureSlot(daysAhead: -2)));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task Given_UnknownCourt_CreateBooking_Should_Return404()
    {
        var member = await RegisterMemberAsync();

        var response = await As(member).Bookings.Create(Command(member.Id, Guid.NewGuid(), FutureSlot()));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Given_TooShortSlot_CreateBooking_Should_Return400()
    {
        var member = await RegisterMemberAsync();
        var court = await CreateCourtAsync();

        var response = await As(member).Bookings.Create(Command(member.Id, court.Id, FutureSlot(), minutes: 15));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That((await response.ReadProblemAsync()).Errors.Keys).Contains("end");
    }

    [Test]
    public async Task Given_BookingForAnotherMember_CreateBooking_Should_Return403()
    {
        var member = await RegisterMemberAsync();
        var other = await RegisterMemberAsync();
        var court = await CreateCourtAsync();

        var response = await As(member).Bookings.Create(Command(other.Id, court.Id, FutureSlot()));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Given_OwnBooking_Cancel_Should_StoreReasonAndFreeTheSlot()
    {
        var member = await RegisterMemberAsync();
        var court = await CreateCourtAsync();
        var start = FutureSlot();
        var booking = await BookAsync(member, court, start);

        var cancelled = (await As(member).Bookings.Cancel(booking.Id, new CancelBookingRequest { Reason = "injury" })).Value();
        var rebooked = await As(member).Bookings.Create(Command(member.Id, court.Id, start));

        await Assert.That(cancelled.Status).IsEqualTo(BookingStatus.Cancelled);
        await Assert.That(cancelled.CancellationReason).IsEqualTo("injury");
        await Assert.That(rebooked.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }

    [Test]
    public async Task Given_BookingOfAnotherMember_Cancel_Should_Return403()
    {
        var owner = await RegisterMemberAsync();
        var other = await RegisterMemberAsync();
        var booking = await BookAsync(owner, await CreateCourtAsync(), FutureSlot());

        var response = await As(other).Bookings.Cancel(booking.Id, new CancelBookingRequest());

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Given_CancelledBooking_Cancel_Should_Return422()
    {
        var member = await RegisterMemberAsync();
        var booking = await BookAsync(member, await CreateCourtAsync(), FutureSlot());
        await As(member).Bookings.Cancel(booking.Id, new CancelBookingRequest());

        var response = await As(member).Bookings.Cancel(booking.Id, new CancelBookingRequest());

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task Given_FutureBooking_Complete_Should_Return422()
    {
        var member = await RegisterMemberAsync();
        var booking = await BookAsync(member, await CreateCourtAsync(), FutureSlot());

        var response = await Admin.Bookings.Complete(booking.Id);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task Given_MemberToken_MarkNoShow_Should_Return403()
    {
        var member = await RegisterMemberAsync();
        var booking = await BookAsync(member, await CreateCourtAsync(), FutureSlot());

        var response = await As(member).Bookings.MarkNoShow(booking.Id);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
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

        await Assert.That(page.Items.Select(b => b.Id)).IsEquivalentTo([own.Id]);
    }

    [Test]
    public async Task Given_MemberFilteringOnAnotherMember_ListBookings_Should_Return403()
    {
        var member = await RegisterMemberAsync();
        var other = await RegisterMemberAsync();

        var response = await As(member).Bookings.List(new BookingListParameters { MemberId = other.Id });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
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

        await Assert.That(page.Items.Select(b => b.Id)).IsEquivalentTo([evening.Id, morning.Id], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Given_UnknownCourt_ListCourtBookings_Should_Return404()
    {
        var response = await Admin.Courts.ListBookings(Guid.NewGuid());

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}
