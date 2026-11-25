using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Bookings;
using Padel.Domain.Common;

namespace Padel.Application.Features.Bookings;

public sealed record CancelBookingCommand(Guid Id, string? Reason = null) : IRequest<BookingDto>;

public sealed class CancelBookingCommandHandler(
    IBookingRepository bookings,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IRequestHandler<CancelBookingCommand, BookingDto>
{
    public async Task<Result<BookingDto>> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetByIdAsync(request.Id, cancellationToken);
        if (booking is null)
        {
            return ApplicationErrors.NotFound<Booking>(request.Id);
        }

        if (currentUser.CanAccessMember(booking.MemberId) is Error accessDenied)
        {
            return accessDenied;
        }

        if (booking.Cancel(request.Reason) is Error cancelledError)
        {
            return cancelledError;
        }

        return await unitOfWork.SaveChangesAsync(cancellationToken) is Error saveError ? saveError : booking.ToDto();
    }
}
