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

    /// <summary>True when the member has a non-cancelled booking (on any court) that overlaps the slot.</summary>
    Task<bool> MemberHasOverlapAsync(Guid memberId, TimeSlot slot, CancellationToken cancellationToken = default);

    /// <summary>The member's confirmed bookings that have not started yet.</summary>
    Task<int> CountUpcomingAsync(Guid memberId, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task<Booking?> FindByIdempotencyKeyAsync(Guid memberId, string idempotencyKey, CancellationToken cancellationToken = default);
}
