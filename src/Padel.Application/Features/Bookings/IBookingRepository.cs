using Padel.Application.Common;
using Padel.Domain.Bookings;
using Padel.Domain.Common;

namespace Padel.Application.Features.Bookings;

public interface IBookingRepository : IRepository<Booking>
{
    /// <summary>
    /// True when a non-cancelled booking on the court overlaps the slot.
    /// </summary>
    Task<bool> HasOverlapAsync(Guid courtId, TimeSlot slot, CancellationToken cancellationToken = default);
}
