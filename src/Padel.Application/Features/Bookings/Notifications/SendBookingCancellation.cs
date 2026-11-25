using Padel.Application.Common.Messaging;
using Padel.Domain.Bookings;

namespace Padel.Application.Features.Bookings.Notifications;

/// <summary>Tells the member that a booking was cancelled (by the member or by an admin).</summary>
public sealed class SendBookingCancellation(BookingNotifications notifications) : DomainEventHandler<BookingCancelled>
{
    public override Task Handle(BookingCancelled @event, CancellationToken cancellationToken) =>
        notifications.SendCancellationAsync(@event.BookingId, cancellationToken);
}
