using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Application.Common.Querying;
using Padel.Application.Features.Courts;
using Padel.Domain.Bookings;
using Padel.Domain.Common;
using Padel.Domain.Courts;

namespace Padel.Application.Features.Bookings;

/// <summary>
/// The schedule of one court, visible to every authenticated user.
/// </summary>
public sealed record ListCourtBookingsQuery : ListQuery, IRequest<PagedResult<BookingDto>>
{
    public Guid CourtId { get; init; }
    public BookingStatus? Status { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
}

public sealed class ListCourtBookingsQueryHandler(
    IBookingRepository bookings,
    ICourtRepository courts) : IRequestHandler<ListCourtBookingsQuery, PagedResult<BookingDto>>
{
    public async Task<Result<PagedResult<BookingDto>>> Handle(ListCourtBookingsQuery request, CancellationToken cancellationToken)
    {
        if (await courts.GetByIdAsync(request.CourtId, cancellationToken) is null)
        {
            return ApplicationErrors.NotFound<Court>(request.CourtId);
        }

        var courtId = request.CourtId;
        var options = ListOptions<Booking>.From(request, BookingSorting.Map)
            .Where(booking => booking.CourtId == courtId)
            .WithStatusAndPeriod(request.Status, request.From, request.To);

        var page = await bookings.ListAsync(options, cancellationToken);
        return page.Map(booking => booking.ToDto());
    }
}
