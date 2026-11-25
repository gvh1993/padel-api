using Microsoft.Extensions.Options;
using Padel.Application.Common.Notifications;
using Padel.Application.Features.Courts;
using Padel.Application.Features.Members;
using Padel.Domain.Bookings;

namespace Padel.Application.Features.Bookings.Notifications;

/// <summary>Composes the booking notifications, with the time in the club's time zone.</summary>
public sealed class BookingNotifications(
    IBookingRepository bookings,
    IMemberRepository members,
    ICourtRepository courts,
    INotificationSender sender,
    IOptions<BookingPolicy> policy)
{
    public Task SendConfirmationAsync(Guid bookingId, CancellationToken cancellationToken) =>
        SendAsync(bookingId, "Booking confirmed", details => $"Your booking of {details} is confirmed.", cancellationToken);

    public Task SendCancellationAsync(Guid bookingId, CancellationToken cancellationToken) =>
        SendAsync(bookingId, "Booking cancelled", details => $"Your booking of {details} is cancelled.", cancellationToken);

    private async Task SendAsync(Guid bookingId, string subject, Func<string, string> body, CancellationToken cancellationToken)
    {
        // Loaded fresh: the event only carries ids, and the data may have changed since.
        var booking = await bookings.GetByIdAsync(bookingId, cancellationToken);
        if (booking is null
            || await members.GetByIdAsync(booking.MemberId, cancellationToken) is not { } member
            || await courts.GetByIdAsync(booking.CourtId, cancellationToken) is not { } court)
        {
            return;
        }

        var start = TimeZoneInfo.ConvertTime(booking.Slot.Start, policy.Value.ClubTimeZone);
        var end = TimeZoneInfo.ConvertTime(booking.Slot.End, policy.Value.ClubTimeZone);
        var details = $"{court.Name} on {start:dddd d MMMM yyyy} from {start:HH:mm} to {end:HH:mm}";

        await sender.SendAsync(new Notification(member.Email.Value, subject, body(details)), cancellationToken);
    }
}
