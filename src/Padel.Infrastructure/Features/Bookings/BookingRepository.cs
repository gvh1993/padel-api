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

    public Task<bool> MemberHasOverlapAsync(Guid memberId, TimeSlot slot, CancellationToken cancellationToken = default)
    {
        var start = slot.Start;
        var end = slot.End;

        return Set.AnyAsync(
            booking => booking.MemberId == memberId
                && booking.Status != BookingStatus.Cancelled
                && booking.Slot.Start < end
                && start < booking.Slot.End,
            cancellationToken);
    }

    public Task<int> CountUpcomingAsync(Guid memberId, DateTimeOffset now, CancellationToken cancellationToken = default) =>
        Set.CountAsync(
            booking => booking.MemberId == memberId
                && booking.Status == BookingStatus.Confirmed
                && booking.Slot.Start > now,
            cancellationToken);

    public Task<Booking?> FindByIdempotencyKeyAsync(Guid memberId, string idempotencyKey, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(
            booking => booking.MemberId == memberId && booking.IdempotencyKey == idempotencyKey,
            cancellationToken);
}
