using Padel.Application.Common;
using Padel.Domain.Common;

namespace Padel.UnitTests.Fakes;

/// <summary>
/// Records saves and collects (then clears) the domain events of all entities in the given stores.
/// </summary>
public sealed class FakeUnitOfWork(params IEnumerable<EntityWithDomainEvents>[] stores) : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public List<IDomainEvent> PublishedEvents { get; } = [];

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;

        foreach (var entity in stores.SelectMany(store => store))
        {
            PublishedEvents.AddRange(entity.DomainEvents);
            entity.ClearDomainEvents();
        }

        return Task.CompletedTask;
    }
}
