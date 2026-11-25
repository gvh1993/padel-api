using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Bookings;
using Padel.Domain.Common;

namespace Padel.Application.Features.Bookings;

public sealed record GetBookingQuery(Guid Id) : IRequest<BookingDto>;

public sealed class GetBookingQueryHandler(
    IBookingRepository bookings,
    ICurrentUser currentUser) : IRequestHandler<GetBookingQuery, BookingDto>
{
    public async Task<Result<BookingDto>> Handle(GetBookingQuery request, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetByIdAsync(request.Id, cancellationToken);
        if (booking is null)
        {
            return ApplicationErrors.NotFound<Booking>(request.Id);
        }

        return currentUser.CanAccessMember(booking.MemberId) is Error accessDenied ? accessDenied : booking.ToDto();
    }
}
