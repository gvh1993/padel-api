using Padel.Application.Common.Messaging;
using Padel.Domain.Bookings;

namespace Padel.Application.Features.Bookings.Notifications;

/// <summary>Confirms a new booking to the member.</summary>
public sealed class SendBookingConfirmation(BookingNotifications notifications) : DomainEventHandler<BookingCreated>
{
    public override Task Handle(BookingCreated @event, CancellationToken cancellationToken) =>
        notifications.SendConfirmationAsync(@event.BookingId, cancellationToken);
}
