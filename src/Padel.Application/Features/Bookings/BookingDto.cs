using Padel.Domain.Bookings;

namespace Padel.Application.Features.Bookings;

public sealed record BookingDto
{
    public Guid Id { get; init; }

    /// <summary>The row version; the API returns it as the <c>ETag</c>.</summary>
    public uint Version { get; init; }
    public Guid MemberId { get; init; }
    public Guid CourtId { get; init; }
    public DateTimeOffset Start { get; init; }
    public DateTimeOffset End { get; init; }
    public BookingStatus Status { get; init; }
    public string? CancellationReason { get; init; }
    public DateTimeOffset CreatedOn { get; init; }
}
