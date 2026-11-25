using Bogus;
using Padel.Api.Features.Bookings;
using Soenneker.Utils.AutoBogus;

namespace Padel.IntegrationTests.Builders;

/// <summary>
/// <c>CreateBookingRequestBuilder.New().For(member.Id).On(court.Id).StartingAt(start).Generate()</c>:
/// an hour from 10:00 UTC a week from now, for an unknown member and court unless given.
/// </summary>
internal static class CreateBookingRequestBuilder
{
    public static Faker<CreateBookingRequest> New() =>
        new AutoFaker<CreateBookingRequest>()
            .RuleFor(r => r.MemberId, f => f.Random.Guid())
            .RuleFor(r => r.CourtId, f => f.Random.Guid())
            .RuleFor(r => r.Start, _ => TestData.FutureSlot())
            .RuleFor(r => r.End, (_, r) => r.Start.AddHours(1));

    public static Faker<CreateBookingRequest> For(this Faker<CreateBookingRequest> faker, Guid memberId) =>
        faker.RuleFor(r => r.MemberId, memberId);

    public static Faker<CreateBookingRequest> On(this Faker<CreateBookingRequest> faker, Guid courtId) =>
        faker.RuleFor(r => r.CourtId, courtId);

    public static Faker<CreateBookingRequest> StartingAt(this Faker<CreateBookingRequest> faker, DateTimeOffset start, int minutes = 60) =>
        faker
            .RuleFor(r => r.Start, start)
            .RuleFor(r => r.End, start.AddMinutes(minutes));
}
