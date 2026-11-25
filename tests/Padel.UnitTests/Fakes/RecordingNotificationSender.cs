using System.Collections.Concurrent;
using Padel.Application.Common.Notifications;

namespace Padel.UnitTests.Fakes;

public sealed class RecordingNotificationSender : INotificationSender
{
    public ConcurrentQueue<Notification> Sent { get; } = new();

    public Task SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        Sent.Enqueue(notification);
        return Task.CompletedTask;
    }
}
