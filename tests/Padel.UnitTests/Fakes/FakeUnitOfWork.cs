using Padel.Application.Common;
using Padel.Domain.Common;

namespace Padel.UnitTests.Fakes;

/// <summary>
/// Records saves and collects (then clears) the domain events of all entities in the given stores.
/// To simulate a failing save, use <c>Substitute.For&lt;IUnitOfWork&gt;()</c> instead.
/// </summary>
public sealed class FakeUnitOfWork(params IEnumerable<EntityWithDomainEvents>[] stores) : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public List<IDomainEvent> PublishedEvents { get; } = [];

    public Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;

        foreach (var entity in stores.SelectMany(store => store))
        {
            PublishedEvents.AddRange(entity.DomainEvents);
            entity.ClearDomainEvents();
        }

        return Task.FromResult<Result>(Success.Instance);
    }
}
