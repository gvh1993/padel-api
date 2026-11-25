namespace Padel.Domain.Common;

/// <summary>
/// Something that happened to an aggregate. Events are stored in the outbox together with the change
/// (same transaction) and handled after the commit; the outbox records when they occurred.
/// </summary>
public abstract record DomainEvent : IDomainEvent;
