using System.Linq.Expressions;
using Padel.Domain.Common;

namespace Padel.Application.Common.Querying;

/// <summary>
/// Filters, sorting and the cursor position for a repository list call.
/// </summary>
public sealed class ListOptions<T> where T : Entity
{
    private readonly List<Expression<Func<T, bool>>> _filters = [];

    public ListOptions(int pageSize, IReadOnlyList<SortExpression<T>> sorts, CursorPosition? after = null)
    {
        PageSize = pageSize;
        Sorts = sorts;
        After = after;
    }

    public int PageSize { get; }
    public IReadOnlyList<SortExpression<T>> Sorts { get; }

    /// <summary>The page starts after this position; <c>null</c> for the first page.</summary>
    public CursorPosition? After { get; }

    public IReadOnlyList<Expression<Func<T, bool>>> Filters => _filters;

    /// <summary>The query's paging and sorting. The query validator has checked the sort and the cursor.</summary>
    public static ListOptions<T> From(ListQuery query, SortMap<T> sortMap)
    {
        var sorts = sortMap.Resolve(query.Sort);

        CursorPosition? after = null;
        if (query.Cursor is not null && !PageCursor.TryDecode(query.Cursor, sorts, out after))
        {
            throw new InvalidOperationException("The cursor is invalid. Validate the query before handling it.");
        }

        return new ListOptions<T>(query.PageSize, sorts, after);
    }

    public ListOptions<T> Where(Expression<Func<T, bool>> filter)
    {
        _filters.Add(filter);
        return this;
    }

    public ListOptions<T> WhereIf(bool condition, Expression<Func<T, bool>> filter) =>
        condition ? Where(filter) : this;
}
