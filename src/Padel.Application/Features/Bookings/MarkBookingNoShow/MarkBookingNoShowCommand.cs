using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Bookings;

namespace Padel.Application.Features.Bookings;

public sealed record MarkBookingNoShowCommand(Guid Id) : IRequest<BookingDto>;

public sealed class MarkBookingNoShowCommandHandler(
    IBookingRepository bookings,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<MarkBookingNoShowCommand, BookingDto>
{
    public async Task<BookingDto> Handle(MarkBookingNoShowCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Booking>(request.Id);

        booking.MarkAsNoShow(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return booking.ToDto();
    }
}
