using Padel.Api.Http;
using Padel.Application.Features.Bookings;
using Padel.Domain.Bookings;

namespace Padel.Api.Features.Bookings;

/// <summary>One page of bookings.</summary>
public sealed record ListBookingsResponse : PagedResponse<ListBookingsItem>;

/// <summary>A booking in the list.</summary>
public sealed record ListBookingsItem(
    Guid Id,
    Guid MemberId,
    Guid CourtId,
    DateTimeOffset Start,
    DateTimeOffset End,
    BookingStatus Status,
    string? CancellationReason,
    DateTimeOffset CreatedOn)
{
    internal static ListBookingsItem From(BookingDto booking) => new(
        booking.Id,
        booking.MemberId,
        booking.CourtId,
        booking.Start,
        booking.End,
        booking.Status,
        booking.CancellationReason,
        booking.CreatedOn);
}
