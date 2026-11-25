using Microsoft.EntityFrameworkCore;
using Npgsql;
using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Common;

namespace Padel.Infrastructure.Persistence;

/// <summary>
/// Dispatches domain events before committing, so handlers run inside the same transaction:
/// either the changes and every handler's side effects are saved, or nothing is.
/// Handlers must therefore only change data in this database; external side effects need an outbox.
/// </summary>
public sealed class PadelUnitOfWork(
    PadelDbContext context,
    IDomainEventPublisher domainEventPublisher) : IUnitOfWork
{
    private const int MaxDispatchRounds = 10;

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return Success.Instance;
        }
        catch (DbUpdateConcurrencyException)
        {
            return ApplicationErrors.ConcurrentChange;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ExclusionViolation,
        } postgresException)
        {
            // The database guards what a concurrent request could slip past the handler's own checks.
            return DatabaseConstraints.ErrorFor(postgresException.ConstraintName);
        }
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        // Handlers may raise new events, so keep dispatching until none are left.
        for (var round = 0; round < MaxDispatchRounds; round++)
        {
            var entities = context.ChangeTracker.Entries<EntityWithDomainEvents>()
                .Select(entry => entry.Entity)
                .Where(entity => entity.DomainEvents.Count > 0)
                .ToList();

            if (entities.Count == 0)
            {
                return;
            }

            var events = entities.SelectMany(entity => entity.DomainEvents).ToList();
            entities.ForEach(entity => entity.ClearDomainEvents());

            foreach (var domainEvent in events)
            {
                await domainEventPublisher.PublishAsync(domainEvent, cancellationToken);
            }
        }

        throw new InvalidOperationException($"Domain events were still being raised after {MaxDispatchRounds} dispatch rounds.");
    }
}
