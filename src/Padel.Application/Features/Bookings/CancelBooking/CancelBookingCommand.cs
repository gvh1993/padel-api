using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Bookings;

namespace Padel.Application.Features.Bookings;

public sealed record CancelBookingCommand(Guid Id, string? Reason = null) : IRequest<BookingDto>;

public sealed class CancelBookingCommandHandler(
    IBookingRepository bookings,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IRequestHandler<CancelBookingCommand, BookingDto>
{
    public async Task<BookingDto> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Booking>(request.Id);

        currentUser.EnsureCanAccessMember(booking.MemberId);

        booking.Cancel(request.Reason);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return booking.ToDto();
    }
}
