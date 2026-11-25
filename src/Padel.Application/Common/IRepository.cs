using Padel.Application.Common.Querying;
using Padel.Domain.Common;

namespace Padel.Application.Common;

public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<T>> ListAsync(ListOptions<T> options, CancellationToken cancellationToken = default);

    void Add(T entity);
}
