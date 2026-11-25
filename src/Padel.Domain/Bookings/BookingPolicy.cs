using Padel.Domain.Common;

namespace Padel.Domain.Bookings;

/// <summary>
/// The club's booking rules. Opening hours are in the club's own time zone, so a club in
/// Amsterdam opens at 07:00 Amsterdam time in winter and in summer.
/// </summary>
public sealed record BookingPolicy
{
    /// <summary>IANA time zone of the club, e.g. <c>Europe/Amsterdam</c>.</summary>
    public string TimeZone { get; init; } = "Europe/Amsterdam";

    public TimeOnly OpensAt { get; init; } = new(7, 0);

    /// <summary>The last booking ends at or before this time, on the same day.</summary>
    public TimeOnly ClosesAt { get; init; } = new(23, 0);

    /// <summary>Bookings start and end on a multiple of this many minutes (e.g. 30: on the hour or half hour).</summary>
    public int SlotMinutes { get; init; } = 30;

    /// <summary>How many bookings a member may have that have not started yet.</summary>
    public int MaxUpcomingBookingsPerMember { get; init; } = 3;

    /// <summary>How long before the start a member can still cancel. Admins can cancel until the start.</summary>
    public TimeSpan CancellationNotice { get; init; } = TimeSpan.FromHours(2);

    public TimeZoneInfo ClubTimeZone => TimeZoneInfo.FindSystemTimeZoneById(TimeZone);

    /// <summary>True when the policy is usable: a known time zone, sensible hours and positive limits.</summary>
    public bool IsValid() =>
        TimeZoneInfo.TryFindSystemTimeZoneById(TimeZone, out _)
        && OpensAt < ClosesAt
        && SlotMinutes is > 0 and <= 60 && 60 % SlotMinutes == 0
        && MaxUpcomingBookingsPerMember > 0
        && CancellationNotice >= TimeSpan.Zero;

    /// <summary>Checks the slot against the opening hours and the slot boundaries, in the club's time zone.</summary>
    public Result Allows(TimeSlot slot)
    {
        var start = TimeZoneInfo.ConvertTime(slot.Start, ClubTimeZone);
        var end = TimeZoneInfo.ConvertTime(slot.End, ClubTimeZone);

        if (start.Date != end.Date || TimeOnly.FromDateTime(start.DateTime) < OpensAt || TimeOnly.FromDateTime(end.DateTime) > ClosesAt)
        {
            return BookingErrors.OutsideOpeningHours(OpensAt, ClosesAt, TimeZone);
        }

        if (!IsOnBoundary(start) || !IsOnBoundary(end))
        {
            return BookingErrors.NotOnSlotBoundary(SlotMinutes);
        }

        return Success.Instance;
    }

    private bool IsOnBoundary(DateTimeOffset time) =>
        time.Minute % SlotMinutes == 0 && time.Second == 0 && time.Millisecond == 0 && time.Microsecond == 0;
}
