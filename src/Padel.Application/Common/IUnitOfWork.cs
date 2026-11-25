using Padel.Domain.Common;

namespace Padel.Application.Common;

public interface IUnitOfWork
{
    /// <summary>
    /// Persists all changes atomically, together with the domain events they raised (the outbox);
    /// the events are handled after the commit.
    /// Unique-constraint and concurrency violations fail with an <see cref="ConflictError"/>.
    /// </summary>
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default);
}
