namespace Padel.Infrastructure.Persistence.Outbox;

/// <summary>A domain event, stored in the same transaction as the change that raised it.</summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; }

    /// <summary>The event's type name (<see cref="Type.FullName"/>) in the domain assembly.</summary>
    public required string Type { get; init; }

    /// <summary>The event as JSON.</summary>
    public required string Payload { get; init; }

    public DateTimeOffset OccurredOn { get; init; }

    public DateTimeOffset? ProcessedOn { get; set; }

    public int Attempts { get; set; }

    public string? LastError { get; set; }
}
