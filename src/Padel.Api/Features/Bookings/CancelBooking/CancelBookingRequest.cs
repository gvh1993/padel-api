namespace Padel.Api.Features.Bookings;

/// <summary>Optional body of <c>POST /api/v1/bookings/{id}/cancel</c>.</summary>
public sealed record CancelBookingRequest
{
    public string? Reason { get; init; }
}
