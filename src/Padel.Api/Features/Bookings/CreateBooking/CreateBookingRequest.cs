namespace Padel.Api.Features.Bookings;

/// <summary>Body of <c>POST /api/bookings</c>.</summary>
public sealed record CreateBookingRequest
{
    public required Guid MemberId { get; init; }
    public required Guid CourtId { get; init; }
    public required DateTimeOffset Start { get; init; }
    public required DateTimeOffset End { get; init; }
}
