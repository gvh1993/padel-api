using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Padel.Infrastructure.Persistence.Outbox;

/// <summary>
/// Keeps the outbox small: deletes handled messages once they are older than <see cref="OutboxOptions.Retention"/>.
/// Safe to run on several instances at once; a message is simply deleted by whichever gets there first.
/// </summary>
internal sealed partial class OutboxCleaner(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxCleaner> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.CleanupInterval, timeProvider);

        do
        {
            try
            {
                var deleted = await CleanUpAsync(stoppingToken);
                if (deleted > 0)
                {
                    LogDeleted(deleted);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogCleanupFailed(exception);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>Deletes the handled messages older than the retention; returns how many.</summary>
    public async Task<int> CleanUpAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PadelDbContext>();
        var cutoff = timeProvider.GetUtcNow() - options.Value.Retention;

        return await context.OutboxMessages
            .Where(message => message.ProcessedOn != null && message.ProcessedOn < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted {Count} handled outbox messages")]
    private partial void LogDeleted(int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Cleaning up the outbox failed")]
    private partial void LogCleanupFailed(Exception exception);
}
