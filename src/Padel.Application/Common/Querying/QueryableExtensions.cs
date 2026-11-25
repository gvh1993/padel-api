using Padel.Domain.Common;

namespace Padel.Application.Common.Querying;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, ListOptions<T> options) where T : Entity =>
        options.Filters.Aggregate(query, (current, filter) => current.Where(filter));

    /// <summary>
    /// Applies the requested sort order, with the id as final tie-breaker so paging is stable.
    /// </summary>
    public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, ListOptions<T> options) where T : Entity
    {
        IOrderedQueryable<T>? ordered = null;

        foreach (var sort in options.Sorts)
        {
            ordered = (ordered, sort.Descending) switch
            {
                (null, false) => query.OrderBy(sort.KeySelector),
                (null, true) => query.OrderByDescending(sort.KeySelector),
                (_, false) => ordered.ThenBy(sort.KeySelector),
                (_, true) => ordered.ThenByDescending(sort.KeySelector),
            };
        }

        return ordered is null ? query.OrderBy(e => e.Id) : ordered.ThenBy(e => e.Id);
    }

    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, ListOptions<T> options) where T : Entity =>
        query.Skip(options.Skip).Take(options.PageSize);
}
