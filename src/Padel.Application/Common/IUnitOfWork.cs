using Padel.Domain.Common;

namespace Padel.Application.Common;

public interface IUnitOfWork
{
    /// <summary>
    /// Dispatches pending domain events and persists all changes atomically.
    /// Unique-constraint and concurrency violations fail with an <see cref="ConflictError"/>.
    /// </summary>
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default);
}
