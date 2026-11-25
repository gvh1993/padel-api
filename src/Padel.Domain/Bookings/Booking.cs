using Padel.Domain.Common;
using Padel.Domain.Courts;
using Padel.Domain.Members;

namespace Padel.Domain.Bookings;

public class Booking : EntityWithDomainEvents
{
    private Booking(Guid memberId, Guid courtId, TimeSlot slot, DateTimeOffset createdOn, string? idempotencyKey)
    {
        MemberId = memberId;
        CourtId = courtId;
        Slot = slot;
        Status = BookingStatus.Confirmed;
        CreatedOn = createdOn;
        IdempotencyKey = idempotencyKey;

        AddDomainEvent(new BookingCreated(Id, MemberId, CourtId));
    }

    private Booking()
    {
    }

    public Guid MemberId { get; private set; }
    public Guid CourtId { get; private set; }
    public TimeSlot Slot { get; private set; } = null!;
    public BookingStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset CreatedOn { get; private set; }

    /// <summary>The client's key for this booking request, so a retried request does not book twice.</summary>
    public string? IdempotencyKey { get; private set; }

    /// <param name="member">Who books.</param>
    /// <param name="court">The court to book.</param>
    /// <param name="slot">When.</param>
    /// <param name="now">The current time.</param>
    /// <param name="policy">The club's booking rules.</param>
    /// <param name="upcomingBookingsOfMember">How many bookings the member has that have not started yet.</param>
    /// <param name="idempotencyKey">Optional key of the client request.</param>
    public static Result<Booking> Create(
        Member member,
        Court court,
        TimeSlot slot,
        DateTimeOffset now,
        BookingPolicy policy,
        int upcomingBookingsOfMember,
        string? idempotencyKey = null)
    {
        if (!member.CanBook)
        {
            return BookingErrors.MemberSuspended;
        }

        if (!court.CanBeBooked)
        {
            return BookingErrors.CourtNotAvailable(court.Name, court.Status);
        }

        if (slot.Start <= now)
        {
            return BookingErrors.NotInFuture;
        }

        if (policy.Allows(slot) is Error outsideRules)
        {
            return outsideRules;
        }

        if (upcomingBookingsOfMember >= policy.MaxUpcomingBookingsPerMember)
        {
            return BookingErrors.TooManyUpcomingBookings(policy.MaxUpcomingBookingsPerMember);
        }

        return new Booking(member.Id, court.Id, slot, now, idempotencyKey);
    }

    /// <summary>Cancels the booking, at least <paramref name="requiredNotice"/> before it starts.</summary>
    public Result Cancel(string? reason, DateTimeOffset now, TimeSpan requiredNotice)
    {
        if (Status == BookingStatus.Cancelled)
        {
            return BookingErrors.AlreadyCancelled;
        }

        if (Status is BookingStatus.Completed or BookingStatus.NoShow)
        {
            return BookingErrors.CannotCancel(Status);
        }

        if (now >= Slot.Start)
        {
            return BookingErrors.AlreadyStarted;
        }

        if (Slot.Start - now < requiredNotice)
        {
            return BookingErrors.CancellationTooLate(requiredNotice);
        }

        var cancellationReason = Reason.Create(reason);
        if (cancellationReason is not Reason validReason)
        {
            return cancellationReason.Error;
        }

        Status = BookingStatus.Cancelled;
        CancellationReason = validReason.Text;
        AddDomainEvent(new BookingCancelled(Id, CancellationReason));
        return Success.Instance;
    }

    public Result Complete(DateTimeOffset now)
    {
        if (EnsureConfirmedAndStarted(now, "completed") is Error error)
        {
            return error;
        }

        Status = BookingStatus.Completed;
        AddDomainEvent(new BookingCompleted(Id));
        return Success.Instance;
    }

    public Result MarkAsNoShow(DateTimeOffset now)
    {
        if (EnsureConfirmedAndStarted(now, "marked as no-show") is Error error)
        {
            return error;
        }

        Status = BookingStatus.NoShow;
        AddDomainEvent(new BookingNoShow(Id));
        return Success.Instance;
    }

    /// <summary>True when this booking was made for the same court and slot (an idempotent retry).</summary>
    public bool IsFor(Guid courtId, TimeSlot slot) => CourtId == courtId && Slot == slot;

    public bool Overlaps(Booking other)
    {
        if (CourtId != other.CourtId)
        {
            return false;
        }

        if (Status == BookingStatus.Cancelled || other.Status == BookingStatus.Cancelled)
        {
            return false;
        }

        return Slot.Overlaps(other.Slot);
    }

    private Result EnsureConfirmedAndStarted(DateTimeOffset now, string action)
    {
        if (Status != BookingStatus.Confirmed)
        {
            return BookingErrors.NotConfirmed(action);
        }

        if (now < Slot.Start)
        {
            return BookingErrors.NotStarted(action);
        }

        return Success.Instance;
    }
}
