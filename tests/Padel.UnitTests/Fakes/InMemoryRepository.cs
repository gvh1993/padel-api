using Padel.Application.Common;
using Padel.Application.Common.Querying;
using Padel.Domain.Common;

namespace Padel.UnitTests.Fakes;

/// <summary>
/// In-memory repository that applies <see cref="ListOptions{T}"/> with the same LINQ as EF Core.
/// </summary>
public abstract class InMemoryRepository<T> : IRepository<T> where T : Entity
{
    public List<T> Store { get; } = [];

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Store.FirstOrDefault(entity => entity.Id == id));

    public Task<PagedResult<T>> ListAsync(ListOptions<T> options, CancellationToken cancellationToken = default)
    {
        var window = Store.AsQueryable().ApplyFilters(options).ApplySorting(options).ApplyPaging(options).ToList();

        return Task.FromResult(PagedResult.FromWindow(window, options));
    }

    public void Add(T entity) => Store.Add(entity);
}
