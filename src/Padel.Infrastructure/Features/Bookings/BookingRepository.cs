using Microsoft.EntityFrameworkCore;
using Padel.Application.Features.Bookings;
using Padel.Domain.Bookings;
using Padel.Domain.Common;
using Padel.Infrastructure.Persistence;

namespace Padel.Infrastructure.Features.Bookings;

public sealed class BookingRepository(PadelDbContext context)
    : EfRepository<Booking>(context), IBookingRepository
{
    public Task<bool> HasOverlapAsync(Guid courtId, TimeSlot slot, CancellationToken cancellationToken = default)
    {
        var start = slot.Start;
        var end = slot.End;

        return Set.AnyAsync(
            booking => booking.CourtId == courtId
                && booking.Status != BookingStatus.Cancelled
                && booking.Slot.Start < end
                && start < booking.Slot.End,
            cancellationToken);
    }
}
