using Padel.Domain.Common;

namespace Padel.Application.Common.Messaging;

public interface IDomainEventPublisher
{
    Task PublishAsync(IDomainEvent @event, CancellationToken cancellationToken = default);
}
