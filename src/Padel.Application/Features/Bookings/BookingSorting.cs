using Padel.Application.Common.Querying;
using Padel.Domain.Bookings;

namespace Padel.Application.Features.Bookings;

public static class BookingSorting
{
    public static readonly SortMap<Booking> Map = new SortMap<Booking>("start")
        .Add("start", booking => booking.Slot.Start)
        .Add("end", booking => booking.Slot.End)
        .Add("status", booking => booking.Status)
        .Add("createdOn", booking => booking.CreatedOn);
}
