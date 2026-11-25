using Padel.Application.Features.Bookings;
using Padel.Domain.Bookings;

namespace Padel.Api.Features.Bookings;

/// <summary>A booking.</summary>
public sealed record GetBookingResponse(
    Guid Id,
    Guid MemberId,
    Guid CourtId,
    DateTimeOffset Start,
    DateTimeOffset End,
    BookingStatus Status,
    string? CancellationReason,
    DateTimeOffset CreatedOn)
{
    internal static GetBookingResponse From(BookingDto booking) => new(
        booking.Id,
        booking.MemberId,
        booking.CourtId,
        booking.Start,
        booking.End,
        booking.Status,
        booking.CancellationReason,
        booking.CreatedOn);
}
