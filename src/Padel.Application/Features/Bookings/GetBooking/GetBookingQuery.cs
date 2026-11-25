using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Bookings;

namespace Padel.Application.Features.Bookings;

public sealed record GetBookingQuery(Guid Id) : IRequest<BookingDto>;

public sealed class GetBookingQueryHandler(
    IBookingRepository bookings,
    ICurrentUser currentUser) : IRequestHandler<GetBookingQuery, BookingDto>
{
    public async Task<BookingDto> Handle(GetBookingQuery request, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Booking>(request.Id);

        currentUser.EnsureCanAccessMember(booking.MemberId);

        return booking.ToDto();
    }
}
