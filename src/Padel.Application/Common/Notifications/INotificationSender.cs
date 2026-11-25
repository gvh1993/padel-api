namespace Padel.Application.Common.Notifications;

/// <summary>Sends notifications to members (email, push, ...).</summary>
public interface INotificationSender
{
    Task SendAsync(Notification notification, CancellationToken cancellationToken = default);
}
