using Padel.Domain.Common;
using Padel.Domain.Courts;

namespace Padel.Domain.Bookings;

public static class BookingErrors
{
    public static readonly Error MemberSuspended = Error.BusinessRule(
        "Booking.MemberSuspended",
        "A suspended member cannot make bookings.");

    public static readonly Error NotInFuture = Error.BusinessRule(
        "Booking.NotInFuture",
        "A booking must start in the future.");

    public static readonly Error AlreadyCancelled = Error.BusinessRule(
        "Booking.AlreadyCancelled",
        "Booking is already cancelled.");

    public static readonly Error SlotTaken = Error.Conflict(
        "Booking.SlotTaken",
        "The court is already booked for the requested time slot.");

    public static Error CourtNotAvailable(string courtName, CourtStatus status) =>
        Error.BusinessRule("Booking.CourtNotAvailable", $"Court '{courtName}' is not available for booking ({status}).");

    public static Error CannotCancel(BookingStatus status) =>
        Error.BusinessRule("Booking.CannotCancel", $"A booking with status {status} cannot be cancelled.");

    public static Error NotConfirmed(string action) =>
        Error.BusinessRule("Booking.NotConfirmed", $"Only confirmed bookings can be {action}.");

    public static Error NotStarted(string action) =>
        Error.BusinessRule("Booking.NotStarted", $"A booking cannot be {action} before it has started.");
}
