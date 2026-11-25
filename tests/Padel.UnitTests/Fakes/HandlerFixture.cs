using Microsoft.Extensions.Time.Testing;
using Padel.Domain.Bookings;
using Padel.Domain.Common;
using Padel.Domain.Courts;
using Padel.Domain.Members;

namespace Padel.UnitTests.Fakes;

/// <summary>
/// Repositories, unit of work, clock and caller for handler tests. Acts as admin unless changed.
/// </summary>
public sealed class HandlerFixture
{
    public static readonly DateTimeOffset Now = new(2026, 11, 25, 9, 0, 0, TimeSpan.Zero);

    public HandlerFixture()
    {
        UnitOfWork = new FakeUnitOfWork(Members.Store, Courts.Store, Bookings.Store);
    }

    public InMemoryMemberRepository Members { get; } = new();
    public InMemoryCourtRepository Courts { get; } = new();
    public InMemoryBookingRepository Bookings { get; } = new();
    public FakeUnitOfWork UnitOfWork { get; }
    public FakeTimeProvider Clock { get; } = new(Now);
    public FakeCurrentUser CurrentUser { get; set; } = FakeCurrentUser.Admin();

    public Member AddMember(string email = "gj@example.com", string firstName = "Gert-Jan", string lastName = "De Vries")
    {
        var member = new Member(new FullName(firstName, lastName), new Email(email), null, Now.AddDays(-30));
        member.ClearDomainEvents();
        Members.Store.Add(member);
        return member;
    }

    public Court AddCourt(string name = "Court A", string surface = "glass")
    {
        var court = new Court(name, surface);
        court.ClearDomainEvents();
        Courts.Store.Add(court);
        return court;
    }

    public Booking AddBooking(Member member, Court court, DateTimeOffset start, TimeSpan? duration = null)
    {
        var slot = new TimeSlot(start, start + (duration ?? TimeSpan.FromHours(1)));
        var booking = Booking.Create(member, court, slot, Now);
        booking.ClearDomainEvents();
        Bookings.Store.Add(booking);
        return booking;
    }
}
