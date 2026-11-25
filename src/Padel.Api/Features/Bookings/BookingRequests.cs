namespace Padel.Api.Features.Bookings;

public sealed record CancelBookingRequest
{
    public string? Reason { get; init; }
}
