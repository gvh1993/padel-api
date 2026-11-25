using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Padel.Application.Common;
using Padel.Application.Common.Messaging;

namespace Padel.Infrastructure.Persistence.Outbox;

/// <summary>
/// Hands the stored domain events to their handlers, after the change that raised them was committed.
/// Several instances can run side by side: each batch is locked with <c>FOR UPDATE SKIP LOCKED</c>.
/// A handler that fails is retried on the next round, up to <see cref="OutboxOptions.MaxAttempts"/> times.
/// </summary>
internal sealed partial class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            int processed;
            try
            {
                processed = await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogBatchFailed(exception);
                processed = 0;
            }

            if (processed == 0)
            {
                await Task.Delay(options.Value.PollingInterval, timeProvider, stoppingToken);
            }
        }
    }

    /// <summary>Handles one batch of messages; returns how many there were.</summary>
    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PadelDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();
        var settings = options.Value;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var messages = await context.OutboxMessages
            .FromSql($"""
                SELECT * FROM "OutboxMessages"
                WHERE "ProcessedOn" IS NULL AND "Attempts" < {settings.MaxAttempts}
                ORDER BY "OccurredOn"
                LIMIT {settings.BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            using var activity = PadelTelemetry.ActivitySource.StartActivity($"Outbox {message.Type}");

            try
            {
                await publisher.PublishAsync(OutboxSerializer.ToEvent(message), cancellationToken);
                message.ProcessedOn = timeProvider.GetUtcNow();
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                message.Attempts++;
                message.LastError = exception.Message.Length > 2000 ? exception.Message[..2000] : exception.Message;
                activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
                LogMessageFailed(exception, message.Id, message.Type, message.Attempts);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return messages.Count;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Processing the outbox failed")]
    private partial void LogBatchFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox message {MessageId} ({MessageType}) failed, attempt {Attempts}")]
    private partial void LogMessageFailed(Exception exception, Guid messageId, string messageType, int attempts);
}
