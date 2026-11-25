using Bogus;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Padel.Domain.Bookings;
using Padel.Domain.Courts;
using Padel.Domain.Members;
using Padel.UnitTests.Builders;

namespace Padel.UnitTests.Fakes;

/// <summary>
/// Repositories, unit of work, clock and caller for handler tests. Acts as admin unless changed.
/// Put test data in the repositories with <c>Add(MemberBuilder.New()...)</c>.
/// </summary>
public sealed class HandlerFixture
{
    public static readonly DateTimeOffset Now = TestData.Now;

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

    /// <summary>The default club rules (Amsterdam, 07:00-23:00, half hours, 3 upcoming bookings, 2 hours notice).</summary>
    public BookingPolicy Policy { get; set; } = new();

    public IOptions<BookingPolicy> PolicyOptions => Options.Create(Policy);

    public Member Add(Faker<Member> builder)
    {
        var member = builder.Generate();
        Members.Store.Add(member);
        return member;
    }

    public Court Add(Faker<Court> builder)
    {
        var court = builder.Generate();
        Courts.Store.Add(court);
        return court;
    }

    public Booking Add(Faker<Booking> builder)
    {
        var booking = builder.Generate();
        Bookings.Store.Add(booking);
        return booking;
    }
}
