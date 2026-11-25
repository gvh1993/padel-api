using Padel.Api.Http;
using Padel.Application.Features.Bookings;
using Padel.Domain.Bookings;

namespace Padel.Api.Features.Bookings;

/// <summary>One page of the schedule of a court.</summary>
public sealed record ListCourtBookingsResponse : PagedResponse<ListCourtBookingsItem>;

/// <summary>A booking on the court.</summary>
public sealed record ListCourtBookingsItem(
    Guid Id,
    Guid MemberId,
    Guid CourtId,
    DateTimeOffset Start,
    DateTimeOffset End,
    BookingStatus Status,
    string? CancellationReason,
    DateTimeOffset CreatedOn)
{
    internal static ListCourtBookingsItem From(BookingDto booking) => new(
        booking.Id,
        booking.MemberId,
        booking.CourtId,
        booking.Start,
        booking.End,
        booking.Status,
        booking.CancellationReason,
        booking.CreatedOn);
}
