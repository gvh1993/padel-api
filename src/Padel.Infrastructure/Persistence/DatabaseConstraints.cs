namespace Padel.Infrastructure.Persistence;

/// <summary>
/// Names of database constraints that protect business rules, with the message shown to API clients.
/// </summary>
internal static class DatabaseConstraints
{
    public const string MemberEmailUnique = "IX_Members_Email";
    public const string BookingNoOverlap = "EX_Bookings_NoOverlappingSlots";

    public const string RowVersionColumn = "Version";

    public static string MessageFor(string? constraintName) => constraintName switch
    {
        MemberEmailUnique => "A member with this email already exists.",
        BookingNoOverlap => "The court is already booked for the requested time slot.",
        _ => "The request conflicts with existing data.",
    };
}
