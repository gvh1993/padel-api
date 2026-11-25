using Padel.Application.Common.Querying;
using Padel.Domain.Bookings;

namespace Padel.Application.Features.Bookings;

internal static class BookingFilters
{
    /// <summary>
    /// Status filter plus a period filter: a booking matches when its slot overlaps [from, to).
    /// </summary>
    public static ListOptions<Booking> WithStatusAndPeriod(
        this ListOptions<Booking> options,
        BookingStatus? status,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        var fromUtc = from?.ToUniversalTime();
        var toUtc = to?.ToUniversalTime();

        return options
            .WhereIf(status.HasValue, booking => booking.Status == status)
            .WhereIf(fromUtc.HasValue, booking => booking.Slot.End > fromUtc)
            .WhereIf(toUtc.HasValue, booking => booking.Slot.Start < toUtc);
    }
}
