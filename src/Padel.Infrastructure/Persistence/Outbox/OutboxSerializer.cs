using System.Text.Json;
using Padel.Domain.Common;

namespace Padel.Infrastructure.Persistence.Outbox;

internal static class OutboxSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static OutboxMessage ToMessage(IDomainEvent domainEvent, DateTimeOffset occurredOn) => new()
    {
        Id = Guid.CreateVersion7(occurredOn),
        Type = domainEvent.GetType().FullName!,
        Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), Options),
        OccurredOn = occurredOn,
    };

    public static IDomainEvent ToEvent(OutboxMessage message)
    {
        var type = typeof(DomainEvent).Assembly.GetType(message.Type)
            ?? throw new InvalidOperationException($"Unknown domain event type '{message.Type}'.");

        return (IDomainEvent)(JsonSerializer.Deserialize(message.Payload, type, Options)
            ?? throw new InvalidOperationException($"Outbox message {message.Id} has no payload."));
    }
}
