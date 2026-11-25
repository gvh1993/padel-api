using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Bookings;
using Padel.Domain.Common;

namespace Padel.Application.Features.Bookings;

public sealed record CompleteBookingCommand(Guid Id) : IRequest<BookingDto>;

public sealed class CompleteBookingCommandHandler(
    IBookingRepository bookings,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<CompleteBookingCommand, BookingDto>
{
    public async Task<Result<BookingDto>> Handle(CompleteBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetByIdAsync(request.Id, cancellationToken);
        if (booking is null)
        {
            return ApplicationErrors.NotFound<Booking>(request.Id);
        }

        if (booking.Complete(timeProvider.GetUtcNow()) is Error changedError)
        {
            return changedError;
        }

        return await unitOfWork.SaveChangesAsync(cancellationToken) is Error saveError ? saveError : booking.ToDto();
    }
}
