using Padel.Domain.Common;

namespace Padel.Application.Common.Messaging;

public interface IDomainEventHandlerDispatch
{
    Task Dispatch(IDomainEvent @event, CancellationToken cancellationToken);
}

public interface IDomainEventHandler<TEvent> : IDomainEventHandlerDispatch where TEvent : IDomainEvent
{
    Task Handle(TEvent @event, CancellationToken cancellationToken);
}

public abstract class DomainEventHandler<TEvent> : IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
    public abstract Task Handle(TEvent @event, CancellationToken cancellationToken);

    Task IDomainEventHandlerDispatch.Dispatch(IDomainEvent @event, CancellationToken cancellationToken)
        => Handle((TEvent)@event, cancellationToken);
}
