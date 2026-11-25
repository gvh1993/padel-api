using Microsoft.Extensions.Options;
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

    /// <summary>Optional client key (the <c>Idempotency-Key</c> header): retrying with the same key returns the same booking.</summary>
    public string? IdempotencyKey { get; init; }
}

public sealed class CreateBookingCommandHandler(
    IBookingRepository bookings,
    IMemberRepository members,
    ICourtRepository courts,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IOptions<BookingPolicy> policy) : IRequestHandler<CreateBookingCommand, BookingDto>
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

        if (request.IdempotencyKey is { } key
            && await bookings.FindByIdempotencyKeyAsync(member.Id, key, cancellationToken) is { } earlier)
        {
            // A retry of a request that already succeeded: return the same booking, unless the key was reused.
            return earlier.IsFor(court.Id, slot) ? earlier.ToDto() : BookingErrors.IdempotencyKeyReused;
        }

        var now = timeProvider.GetUtcNow();
        var upcoming = await bookings.CountUpcomingAsync(member.Id, now, cancellationToken);

        var bookingResult = Booking.Create(member, court, slot, now, policy.Value, upcoming, request.IdempotencyKey);
        if (bookingResult is not Booking booking)
        {
            return bookingResult.Error;
        }

        // Fast paths for friendly errors; the database exclusion constraints guard against races.
        if (await bookings.HasOverlapAsync(court.Id, slot, cancellationToken))
        {
            return BookingErrors.SlotTaken;
        }

        if (await bookings.MemberHasOverlapAsync(member.Id, slot, cancellationToken))
        {
            return BookingErrors.MemberAlreadyBooked;
        }

        bookings.Add(booking);
        return await unitOfWork.SaveChangesAsync(cancellationToken) is Error saveError ? saveError : booking.ToDto();
    }
}
