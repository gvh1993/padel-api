using Padel.Application.Features.Bookings;
using Padel.Application.Features.Courts;
using Padel.Application.Features.Members;
using Padel.Domain.Bookings;
using Padel.Domain.Common;
using Padel.Domain.Courts;
using Padel.Domain.Members;

namespace Padel.UnitTests.Fakes;

public sealed class InMemoryMemberRepository : InMemoryRepository<Member>, IMemberRepository
{
    public Task<Member?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        Task.FromResult(Store.FirstOrDefault(member => member.Email == email));
}

public sealed class InMemoryCourtRepository : InMemoryRepository<Court>, ICourtRepository;

public sealed class InMemoryBookingRepository : InMemoryRepository<Booking>, IBookingRepository
{
    public Task<bool> HasOverlapAsync(Guid courtId, TimeSlot slot, CancellationToken cancellationToken = default) =>
        Task.FromResult(Store.Any(booking =>
            booking.CourtId == courtId
            && booking.Status != BookingStatus.Cancelled
            && booking.Slot.Overlaps(slot)));
}
