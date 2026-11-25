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
    public async Task<BookingDto> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        currentUser.EnsureCanAccessMember(request.MemberId);

        var member = await members.GetByIdAsync(request.MemberId, cancellationToken)
            ?? throw NotFoundException.For<Member>(request.MemberId);

        var court = await courts.GetByIdAsync(request.CourtId, cancellationToken)
            ?? throw NotFoundException.For<Court>(request.CourtId);

        var slot = new TimeSlot(request.Start, request.End);
        var booking = Booking.Create(member, court, slot, timeProvider.GetUtcNow());

        // Fast path for a friendly error; the database exclusion constraint guards against races.
        if (await bookings.HasOverlapAsync(court.Id, slot, cancellationToken))
        {
            throw new ConflictException("The court is already booked for the requested time slot.");
        }

        bookings.Add(booking);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return booking.ToDto();
    }
}
