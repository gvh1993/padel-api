using Microsoft.Extensions.DependencyInjection;
using Padel.Domain.Common;

namespace Padel.Application.Common.Messaging;

public sealed class DomainEventPublisher(IServiceProvider provider) : IDomainEventPublisher
{
    public async Task PublishAsync(IDomainEvent @event, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(@event.GetType());

        foreach (var handler in provider.GetServices(handlerType).OfType<IDomainEventHandlerDispatch>())
        {
            await handler.Dispatch(@event, cancellationToken);
        }
    }
}
