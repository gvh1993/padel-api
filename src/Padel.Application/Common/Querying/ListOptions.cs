using System.Linq.Expressions;
using Padel.Domain.Common;

namespace Padel.Application.Common.Querying;

/// <summary>
/// Filters, sorting and paging for a repository list call.
/// </summary>
public sealed class ListOptions<T> where T : Entity
{
    private readonly List<Expression<Func<T, bool>>> _filters = [];

    public ListOptions(int page, int pageSize, IReadOnlyList<SortExpression<T>> sorts)
    {
        Page = page;
        PageSize = pageSize;
        Sorts = sorts;
    }

    public int Page { get; }
    public int PageSize { get; }
    public int Skip => (Page - 1) * PageSize;
    public IReadOnlyList<SortExpression<T>> Sorts { get; }
    public IReadOnlyList<Expression<Func<T, bool>>> Filters => _filters;

    public static ListOptions<T> From(ListQuery query, SortMap<T> sortMap) =>
        new(query.Page, query.PageSize, sortMap.Resolve(query.Sort));

    public ListOptions<T> Where(Expression<Func<T, bool>> filter)
    {
        _filters.Add(filter);
        return this;
    }

    public ListOptions<T> WhereIf(bool condition, Expression<Func<T, bool>> filter) =>
        condition ? Where(filter) : this;
}
