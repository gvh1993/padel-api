using Padel.Application.Features.Bookings;
using Padel.Domain.Bookings;

namespace Padel.Api.Features.Bookings;

/// <summary>The booking, marked as a no-show.</summary>
public sealed record MarkBookingNoShowResponse(
    Guid Id,
    Guid MemberId,
    Guid CourtId,
    DateTimeOffset Start,
    DateTimeOffset End,
    BookingStatus Status,
    string? CancellationReason,
    DateTimeOffset CreatedOn)
{
    internal static MarkBookingNoShowResponse From(BookingDto booking) => new(
        booking.Id,
        booking.MemberId,
        booking.CourtId,
        booking.Start,
        booking.End,
        booking.Status,
        booking.CancellationReason,
        booking.CreatedOn);
}
