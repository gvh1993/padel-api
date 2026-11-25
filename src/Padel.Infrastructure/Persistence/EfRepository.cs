using Microsoft.EntityFrameworkCore;
using Padel.Application.Common;
using Padel.Application.Common.Querying;
using Padel.Domain.Common;

namespace Padel.Infrastructure.Persistence;

public abstract class EfRepository<T>(PadelDbContext context) : IRepository<T>
    where T : Entity
{
    protected DbSet<T> Set { get; } = context.Set<T>();

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public async Task<PagedResult<T>> ListAsync(ListOptions<T> options, CancellationToken cancellationToken = default)
    {
        var window = await Set.AsNoTracking()
            .ApplyFilters(options)
            .ApplySorting(options)
            .ApplyPaging(options)
            .ToListAsync(cancellationToken);

        return PagedResult.FromWindow(window, options);
    }

    public void Add(T entity) => Set.Add(entity);
}
