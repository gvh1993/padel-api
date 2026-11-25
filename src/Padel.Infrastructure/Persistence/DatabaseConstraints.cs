using Padel.Domain.Bookings;
using Padel.Domain.Common;

namespace Padel.Infrastructure.Persistence;

/// <summary>
/// Names of database constraints that protect business rules, with the message shown to API clients.
/// </summary>
internal static class DatabaseConstraints
{
    public const string MemberEmailUnique = "IX_Members_Email";
    public const string BookingNoOverlap = "EX_Bookings_NoOverlappingSlots";
    public const string BookingNoMemberOverlap = "EX_Bookings_NoOverlappingMemberSlots";
    public const string BookingIdempotencyKeyUnique = "IX_Bookings_MemberId_IdempotencyKey";

    public const string RowVersionColumn = "Version";

    public static Error ErrorFor(string? constraintName) => constraintName switch
    {
        MemberEmailUnique => Error.Conflict("Member.EmailInUse", "A member with this email already exists."),
        BookingNoOverlap => BookingErrors.SlotTaken,
        BookingNoMemberOverlap => BookingErrors.MemberAlreadyBooked,
        BookingIdempotencyKeyUnique => BookingErrors.IdempotencyKeyReused,
        _ => Error.Conflict("Database.Conflict", "The request conflicts with existing data."),
    };
}
