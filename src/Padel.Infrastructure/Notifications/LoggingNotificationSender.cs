using Microsoft.Extensions.Logging;
using Padel.Application.Common.Notifications;

namespace Padel.Infrastructure.Notifications;

/// <summary>
/// Writes notifications to the log instead of sending them. Replace it with an email (or other)
/// sender once the club has one; the rest of the flow (outbox, retries) stays the same.
/// </summary>
internal sealed partial class LoggingNotificationSender(ILogger<LoggingNotificationSender> logger) : INotificationSender
{
    public Task SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        LogNotification(notification.To, notification.Subject, notification.Body);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Notification to {To}: {Subject}. {Body}")]
    private partial void LogNotification(string to, string subject, string body);
}
