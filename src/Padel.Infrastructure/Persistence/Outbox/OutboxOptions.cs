namespace Padel.Infrastructure.Persistence.Outbox;

/// <summary>Settings of the outbox processor (section <c>Outbox</c>).</summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>How long the processor waits when there was nothing to do.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(2);

    public int BatchSize { get; set; } = 20;

    /// <summary>After this many failed attempts a message is no longer retried (see <c>LastError</c>).</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// How long handled messages are kept (for troubleshooting) before <see cref="OutboxCleaner"/> deletes them.
    /// Messages that failed <see cref="MaxAttempts"/> times are kept until someone looks at them.
    /// </summary>
    public TimeSpan Retention { get; set; } = TimeSpan.FromDays(7);

    /// <summary>The outbox health check reports Degraded when a message waits longer than this.</summary>
    public TimeSpan StaleAfter { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>How often the cleaner deletes handled messages older than <see cref="Retention"/>.</summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
}
