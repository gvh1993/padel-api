using Padel.Application.Common;
using Padel.Domain.Common;

namespace Padel.UnitTests.Fakes;

/// <summary>
/// Records saves and collects (then clears) the domain events of all entities in the given stores.
/// Set <see cref="FailWith"/> to simulate a failing save (e.g. a concurrency conflict).
/// </summary>
public sealed class FakeUnitOfWork(params IEnumerable<EntityWithDomainEvents>[] stores) : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public List<IDomainEvent> PublishedEvents { get; } = [];

    public Error? FailWith { get; set; }

    public Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (FailWith is { } error)
        {
            return Task.FromResult<Result>(error);
        }

        SaveCount++;

        foreach (var entity in stores.SelectMany(store => store))
        {
            PublishedEvents.AddRange(entity.DomainEvents);
            entity.ClearDomainEvents();
        }

        return Task.FromResult<Result>(Success.Instance);
    }
}
