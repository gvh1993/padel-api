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

    public static readonly Error MemberAlreadyBooked = Error.Conflict(
        "Booking.MemberAlreadyBooked",
        "The member already has a booking at this time.");

    public static readonly Error AlreadyStarted = Error.BusinessRule(
        "Booking.AlreadyStarted",
        "A booking cannot be cancelled after it has started.");

    public static readonly Error IdempotencyKeyReused = Error.Conflict(
        "Booking.IdempotencyKeyReused",
        "This idempotency key was already used for a different booking.");

    public static Error OutsideOpeningHours(TimeOnly opensAt, TimeOnly closesAt, string timeZone) =>
        Error.BusinessRule(
            "Booking.OutsideOpeningHours",
            $"Bookings must be between {opensAt:HH:mm} and {closesAt:HH:mm} ({timeZone}) on the same day.");

    public static Error NotOnSlotBoundary(int slotMinutes) =>
        Error.BusinessRule("Booking.NotOnSlotBoundary", $"Bookings must start and end on a multiple of {slotMinutes} minutes.");

    public static Error TooManyUpcomingBookings(int maximum) =>
        Error.BusinessRule("Booking.TooManyUpcomingBookings", $"A member can have at most {maximum} upcoming bookings.");

    public static Error CancellationTooLate(TimeSpan notice) =>
        Error.BusinessRule("Booking.CancellationTooLate", $"Members can cancel up to {notice.TotalHours:0.#} hours before the start.");

    public static Error CourtNotAvailable(string courtName, CourtStatus status) =>
        Error.BusinessRule("Booking.CourtNotAvailable", $"Court '{courtName}' is not available for booking ({status}).");

    public static Error CannotCancel(BookingStatus status) =>
        Error.BusinessRule("Booking.CannotCancel", $"A booking with status {status} cannot be cancelled.");

    public static Error NotConfirmed(string action) =>
        Error.BusinessRule("Booking.NotConfirmed", $"Only confirmed bookings can be {action}.");

    public static Error NotStarted(string action) =>
        Error.BusinessRule("Booking.NotStarted", $"A booking cannot be {action} before it has started.");
}
