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
}
