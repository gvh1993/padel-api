using Bogus;
using Padel.Domain.Bookings;
using Padel.Domain.Common;
using Padel.Domain.Courts;
using Padel.Domain.Members;
using Soenneker.Utils.AutoBogus;

namespace Padel.UnitTests.Builders;

/// <summary>
/// <c>BookingBuilder.New().For(member).On(court).StartingAt(start).Generate()</c>: a confirmed hour from
/// <see cref="TestData.Tomorrow10"/>, booked at <see cref="TestData.Now"/>.
/// The builder sets state; to test the booking rules themselves, call <see cref="Booking.Create"/>.
/// </summary>
internal static class BookingBuilder
{
    public static Faker<Booking> New() =>
        new AutoFaker<Booking>()
            // AutoFaker cannot call the private constructor; the rules below set every property.
            .CustomInstantiator(_ => Booking.Create(
                MemberBuilder.New().Generate(), CourtBuilder.New().Generate(), Slot(TestData.Tomorrow10, 60), TestData.Now, new BookingPolicy(), 0).ShouldSucceed())
            .RuleFor(b => b.Id, _ => Guid.CreateVersion7())
            .RuleFor(b => b.Version, 0u)
            .RuleFor(b => b.MemberId, f => f.Random.Guid())
            .RuleFor(b => b.CourtId, f => f.Random.Guid())
            .RuleFor(b => b.Slot, Slot(TestData.Tomorrow10, 60))
            .RuleFor(b => b.Status, BookingStatus.Confirmed)
            .RuleFor(b => b.CancellationReason, _ => null)
            .RuleFor(b => b.CreatedOn, TestData.Now)
            .RuleFor(b => b.IdempotencyKey, _ => null)
            .FinishWith((_, b) => b.ClearDomainEvents());

    public static Faker<Booking> For(this Faker<Booking> faker, Member member) =>
        faker.RuleFor(b => b.MemberId, member.Id);

    public static Faker<Booking> On(this Faker<Booking> faker, Court court) =>
        faker.RuleFor(b => b.CourtId, court.Id);

    public static Faker<Booking> StartingAt(this Faker<Booking> faker, DateTimeOffset start, int minutes = 60) =>
        faker.RuleFor(b => b.Slot, Slot(start, minutes));

    public static Faker<Booking> WithIdempotencyKey(this Faker<Booking> faker, string key) =>
        faker.RuleFor(b => b.IdempotencyKey, key);

    public static Faker<Booking> Cancelled(this Faker<Booking> faker, string? reason = null) =>
        faker
            .RuleFor(b => b.Status, BookingStatus.Cancelled)
            .RuleFor(b => b.CancellationReason, _ => reason);

    public static Faker<Booking> Completed(this Faker<Booking> faker) =>
        faker.RuleFor(b => b.Status, BookingStatus.Completed);

    public static Faker<Booking> NoShow(this Faker<Booking> faker) =>
        faker.RuleFor(b => b.Status, BookingStatus.NoShow);

    private static TimeSlot Slot(DateTimeOffset start, int minutes) =>
        TimeSlot.Create(start, start.AddMinutes(minutes)).ShouldSucceed();
}
