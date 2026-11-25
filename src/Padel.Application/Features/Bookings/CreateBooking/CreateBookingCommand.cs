using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Application.Features.Courts;
using Padel.Application.Features.Members;
using Padel.Domain.Bookings;
using Padel.Domain.Common;
using Padel.Domain.Courts;
using Padel.Domain.Members;

namespace Padel.Application.Features.Bookings;

public sealed record CreateBookingCommand : IRequest<BookingDto>
{
    public required Guid MemberId { get; init; }
    public required Guid CourtId { get; init; }
    public required DateTimeOffset Start { get; init; }
    public required DateTimeOffset End { get; init; }
}

public sealed class CreateBookingCommandHandler(
    IBookingRepository bookings,
    IMemberRepository members,
    ICourtRepository courts,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<CreateBookingCommand, BookingDto>
{
    public async Task<Result<BookingDto>> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.CanAccessMember(request.MemberId) is Error accessDenied)
        {
            return accessDenied;
        }

        var member = await members.GetByIdAsync(request.MemberId, cancellationToken);
        if (member is null)
        {
            return ApplicationErrors.NotFound<Member>(request.MemberId);
        }

        var court = await courts.GetByIdAsync(request.CourtId, cancellationToken);
        if (court is null)
        {
            return ApplicationErrors.NotFound<Court>(request.CourtId);
        }

        var slotResult = TimeSlot.Create(request.Start, request.End);
        if (slotResult is not TimeSlot slot)
        {
            return slotResult.Error;
        }

        var bookingResult = Booking.Create(member, court, slot, timeProvider.GetUtcNow());
        if (bookingResult is not Booking booking)
        {
            return bookingResult.Error;
        }

        // Fast path for a friendly error; the database exclusion constraint guards against races.
        if (await bookings.HasOverlapAsync(court.Id, slot, cancellationToken))
        {
            return BookingErrors.SlotTaken;
        }

        bookings.Add(booking);
        return await unitOfWork.SaveChangesAsync(cancellationToken) is Error saveError ? saveError : booking.ToDto();
    }
}
