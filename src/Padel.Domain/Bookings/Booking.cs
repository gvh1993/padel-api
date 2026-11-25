using Padel.Domain.Common;
using Padel.Domain.Courts;
using Padel.Domain.Members;

namespace Padel.Domain.Bookings;

public enum BookingStatus
{
    Confirmed,
    Cancelled,
    Completed,
    NoShow,
}

public class Booking : EntityWithDomainEvents
{
    public Guid MemberId { get; private set; }
    public Guid CourtId { get; private set; }
    public TimeSlot Slot { get; private set; } = null!;
    public BookingStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset CreatedOn { get; private set; }

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

    public static Booking Create(Member member, Court court, TimeSlot slot, DateTimeOffset now)
    {
        if (!member.CanBook)
        {
            throw new DomainException("A suspended member cannot make bookings.");
        }

        if (!court.CanBeBooked)
        {
            throw new DomainException($"Court '{court.Name}' is not available for booking ({court.Status}).");
        }

        if (slot.Start <= now)
        {
            throw new DomainException("A booking must start in the future.");
        }

        return new Booking(member.Id, court.Id, slot, now);
    }

    public void Cancel(string? reason = null)
    {
        if (Status == BookingStatus.Cancelled)
        {
            throw new DomainException("Booking is already cancelled.");
        }

        if (Status is BookingStatus.Completed or BookingStatus.NoShow)
        {
            throw new DomainException($"A booking with status {Status} cannot be cancelled.");
        }

        Status = BookingStatus.Cancelled;
        CancellationReason = Reason.Normalize(reason);
        AddDomainEvent(new BookingCancelled(Id, CancellationReason));
    }

    public void Complete(DateTimeOffset now)
    {
        EnsureConfirmedAndStarted(now, "completed");

        Status = BookingStatus.Completed;
        AddDomainEvent(new BookingCompleted(Id));
    }

    public void MarkAsNoShow(DateTimeOffset now)
    {
        EnsureConfirmedAndStarted(now, "marked as no-show");

        Status = BookingStatus.NoShow;
        AddDomainEvent(new BookingNoShow(Id));
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

    private void EnsureConfirmedAndStarted(DateTimeOffset now, string action)
    {
        if (Status != BookingStatus.Confirmed)
        {
            throw new DomainException($"Only confirmed bookings can be {action}.");
        }

        if (now < Slot.Start)
        {
            throw new DomainException($"A booking cannot be {action} before it has started.");
        }
    }
}
