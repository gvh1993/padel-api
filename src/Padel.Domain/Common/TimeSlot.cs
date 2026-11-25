namespace Padel.Domain.Common;

/// <summary>
/// A half-open time interval [Start, End). Both values are normalized to UTC.
/// </summary>
public sealed record TimeSlot
{
    public static readonly TimeSpan MinimumDuration = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan MaximumDuration = TimeSpan.FromHours(3);

    // Also used by EF Core to materialize stored (already valid) values.
    private TimeSlot(DateTimeOffset start, DateTimeOffset end)
    {
        Start = start;
        End = end;
    }

    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    public TimeSpan Duration => End - Start;

    /// <summary>Validates the slot: the end after the start, and a duration between the minimum and maximum.</summary>
    public static Result<TimeSlot> Create(DateTimeOffset start, DateTimeOffset end)
    {
        var startUtc = start.ToUniversalTime();
        var endUtc = end.ToUniversalTime();

        if (endUtc <= startUtc)
        {
            return Error.Validation("TimeSlot.EndBeforeStart", "Slot end must be after slot start.");
        }

        var duration = endUtc - startUtc;
        if (duration < MinimumDuration)
        {
            return Error.Validation("TimeSlot.TooShort", $"Slot must be at least {MinimumDuration.TotalMinutes} minutes long.");
        }

        if (duration > MaximumDuration)
        {
            return Error.Validation("TimeSlot.TooLong", $"Slot cannot exceed {MaximumDuration.TotalHours} hours.");
        }

        return new TimeSlot(startUtc, endUtc);
    }

    public bool Overlaps(TimeSlot other) => Start < other.End && other.Start < End;
}
