using Microsoft.EntityFrameworkCore;
using Npgsql;
using Padel.Application.Common;
using Padel.Domain.Common;
using Padel.Infrastructure.Persistence.Outbox;

namespace Padel.Infrastructure.Persistence;

/// <summary>
/// Saves the changes together with the domain events they raised (the transactional outbox): either
/// both are stored, or neither. The <see cref="Outbox.OutboxProcessor"/> hands the events to their
/// handlers after the commit, so a failing handler (e.g. a mail server that is down) never undoes a booking.
/// </summary>
public sealed class PadelUnitOfWork(PadelDbContext context, TimeProvider timeProvider) : IUnitOfWork
{
    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StoreDomainEvents();

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

    private void StoreDomainEvents()
    {
        var entities = context.ChangeTracker.Entries<EntityWithDomainEvents>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToList();

        var occurredOn = timeProvider.GetUtcNow();
        foreach (var entity in entities)
        {
            context.OutboxMessages.AddRange(entity.DomainEvents.Select(domainEvent => OutboxSerializer.ToMessage(domainEvent, occurredOn)));
            entity.ClearDomainEvents();
        }
    }
}
