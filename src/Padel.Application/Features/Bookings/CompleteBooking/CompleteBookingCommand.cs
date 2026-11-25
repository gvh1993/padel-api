using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Bookings;

namespace Padel.Application.Features.Bookings;

public sealed record CompleteBookingCommand(Guid Id) : IRequest<BookingDto>;

public sealed class CompleteBookingCommandHandler(
    IBookingRepository bookings,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<CompleteBookingCommand, BookingDto>
{
    public async Task<BookingDto> Handle(CompleteBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Booking>(request.Id);

        booking.Complete(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return booking.ToDto();
    }
}
