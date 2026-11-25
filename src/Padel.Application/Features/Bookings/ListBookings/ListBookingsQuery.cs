using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Application.Common.Querying;
using Padel.Domain.Bookings;
using Padel.Domain.Common;

namespace Padel.Application.Features.Bookings;

/// <summary>
/// Lists bookings. Admins see all bookings; other users only their own.
/// </summary>
public sealed record ListBookingsQuery : ListQuery, IRequest<PagedResult<BookingDto>>
{
    public Guid? MemberId { get; init; }
    public Guid? CourtId { get; init; }
    public BookingStatus? Status { get; init; }

    /// <summary>Only bookings that end after this moment.</summary>
    public DateTimeOffset? From { get; init; }

    /// <summary>Only bookings that start before this moment.</summary>
    public DateTimeOffset? To { get; init; }
}

public sealed class ListBookingsQueryHandler(
    IBookingRepository bookings,
    ICurrentUser currentUser) : IRequestHandler<ListBookingsQuery, PagedResult<BookingDto>>
{
    public async Task<Result<PagedResult<BookingDto>>> Handle(ListBookingsQuery request, CancellationToken cancellationToken)
    {
        // Admins may filter on any member; other users only see their own bookings.
        var memberId = request.MemberId;
        if (!currentUser.IsAdmin)
        {
            if (currentUser.MemberId is not { } ownMemberId || (memberId.HasValue && memberId != ownMemberId))
            {
                return ApplicationErrors.Forbidden;
            }

            memberId = ownMemberId;
        }

        var courtId = request.CourtId;

        var options = ListOptions<Booking>.From(request, BookingSorting.Map)
            .WhereIf(memberId.HasValue, booking => booking.MemberId == memberId)
            .WhereIf(courtId.HasValue, booking => booking.CourtId == courtId)
            .WithStatusAndPeriod(request.Status, request.From, request.To);

        var page = await bookings.ListAsync(options, cancellationToken);
        return page.Map(booking => booking.ToDto());
    }
}
