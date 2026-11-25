namespace Padel.Domain.Common;

/// <summary>
/// A half-open time interval [Start, End). Both values are normalized to UTC.
/// </summary>
public sealed record TimeSlot
{
    public static readonly TimeSpan MinimumDuration = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan MaximumDuration = TimeSpan.FromHours(3);

    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    public TimeSlot(DateTimeOffset start, DateTimeOffset end)
    {
        Start = start.ToUniversalTime();
        End = end.ToUniversalTime();
        Validate(Start, End);
    }

    public TimeSpan Duration => End - Start;

    public bool Overlaps(TimeSlot other) => Start < other.End && other.Start < End;

    private static void Validate(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
        {
            throw new DomainException("Slot end must be after slot start.");
        }

        var duration = end - start;
        if (duration < MinimumDuration)
        {
            throw new DomainException($"Slot must be at least {MinimumDuration.TotalMinutes} minutes long.");
        }

        if (duration > MaximumDuration)
        {
            throw new DomainException($"Slot cannot exceed {MaximumDuration.TotalHours} hours.");
        }
    }
}
