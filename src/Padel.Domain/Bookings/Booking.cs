using Padel.Domain.Common;
using Padel.Domain.Courts;
using Padel.Domain.Members;

namespace Padel.Domain.Bookings;

public class Booking : EntityWithDomainEvents
{
    private Booking(Guid memberId, Guid courtId, TimeSlot slot, DateTimeOffset createdOn)
    {
        MemberId = memberId;
        CourtId = courtId;
        Slot = slot;
        Status = BookingStatus.Confirmed;
        CreatedOn = createdOn;

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

    public static Result<Booking> Create(Member member, Court court, TimeSlot slot, DateTimeOffset now)
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

        return new Booking(member.Id, court.Id, slot, now);
    }

    public Result Cancel(string? reason = null)
    {
        if (Status == BookingStatus.Cancelled)
        {
            return BookingErrors.AlreadyCancelled;
        }

        if (Status is BookingStatus.Completed or BookingStatus.NoShow)
        {
            return BookingErrors.CannotCancel(Status);
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
