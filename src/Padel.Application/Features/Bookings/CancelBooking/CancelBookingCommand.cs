using Microsoft.Extensions.Options;
using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Bookings;
using Padel.Domain.Common;

namespace Padel.Application.Features.Bookings;

public sealed record CancelBookingCommand(Guid Id, string? Reason = null, uint? ExpectedVersion = null) : IRequest<BookingDto>;

public sealed class CancelBookingCommandHandler(
    IBookingRepository bookings,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IOptions<BookingPolicy> policy) : IRequestHandler<CancelBookingCommand, BookingDto>
{
    public async Task<Result<BookingDto>> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetByIdAsync(request.Id, cancellationToken);
        if (booking is null)
        {
            return ApplicationErrors.NotFound<Booking>(request.Id);
        }

        if (booking.MatchesVersion(request.ExpectedVersion) is Error stale)
        {
            return stale;
        }

        if (currentUser.CanAccessMember(booking.MemberId) is Error accessDenied)
        {
            return accessDenied;
        }

        // Members must give notice; admins can cancel until the booking starts.
        var requiredNotice = currentUser.IsAdmin ? TimeSpan.Zero : policy.Value.CancellationNotice;

        if (booking.Cancel(request.Reason, timeProvider.GetUtcNow(), requiredNotice) is Error cancelledError)
        {
            return cancelledError;
        }

        return await unitOfWork.SaveChangesAsync(cancellationToken) is Error saveError ? saveError : booking.ToDto();
    }
}
