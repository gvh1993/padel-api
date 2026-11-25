using System.Collections.Concurrent;
using Padel.Application.Common.Notifications;

namespace Padel.IntegrationTests.Infrastructure;

/// <summary>Collects the notifications the API sends (via the outbox), so tests can wait for them.</summary>
public sealed class RecordingNotificationSender : INotificationSender
{
    private readonly ConcurrentQueue<Notification> _sent = new();

    public Task SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(notification);
        return Task.CompletedTask;
    }

    /// <summary>Waits until a notification matches, or fails after <paramref name="timeout"/>.</summary>
    public async Task<Notification> WaitForAsync(Func<Notification, bool> match, TimeSpan? timeout = null)
    {
        using var cancellation = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(15));
        while (true)
        {
            if (_sent.FirstOrDefault(match) is { } notification)
            {
                return notification;
            }

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("The expected notification was not sent.");
            }
        }
    }
}
