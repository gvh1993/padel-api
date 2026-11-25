namespace Padel.Application.Common;

public interface IUnitOfWork
{
    /// <summary>
    /// Dispatches pending domain events and persists all changes atomically.
    /// Unique-constraint and concurrency violations surface as <see cref="ConflictException"/>.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
