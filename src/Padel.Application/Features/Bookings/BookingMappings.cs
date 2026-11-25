using Padel.Domain.Bookings;

namespace Padel.Application.Features.Bookings;

public static class BookingMappings
{
    public static BookingDto ToDto(this Booking booking) => new()
    {
        Id = booking.Id,
        Version = booking.Version,
        MemberId = booking.MemberId,
        CourtId = booking.CourtId,
        Start = booking.Slot.Start,
        End = booking.Slot.End,
        Status = booking.Status,
        CancellationReason = booking.CancellationReason,
        CreatedOn = booking.CreatedOn,
    };
}
