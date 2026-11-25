using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Bookings;
using Padel.Domain.Common;

namespace Padel.Application.Features.Bookings;

public sealed record MarkBookingNoShowCommand(Guid Id, uint? ExpectedVersion = null) : IRequest<BookingDto>;

public sealed class MarkBookingNoShowCommandHandler(
    IBookingRepository bookings,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<MarkBookingNoShowCommand, BookingDto>
{
    public async Task<Result<BookingDto>> Handle(MarkBookingNoShowCommand request, CancellationToken cancellationToken)
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

        if (booking.MarkAsNoShow(timeProvider.GetUtcNow()) is Error changedError)
        {
            return changedError;
        }

        return await unitOfWork.SaveChangesAsync(cancellationToken) is Error saveError ? saveError : booking.ToDto();
    }
}
