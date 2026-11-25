using Padel.Domain.Common;

namespace Padel.Application.Common.Querying;

/// <summary>
/// One page of results, and the cursor of the next page (<c>null</c> on the last page).
/// </summary>
public sealed record PagedResult<T>
{
    public List<T> Items { get; init; } = [];
    public string? NextCursor { get; init; }

    public static PagedResult<T> Create(IEnumerable<T> items, string? nextCursor) => new()
    {
        Items = items.ToList(),
        NextCursor = nextCursor,
    };

    public PagedResult<TOut> Map<TOut>(Func<T, TOut> map) =>
        PagedResult<TOut>.Create(Items.Select(map), NextCursor);
}

public static class PagedResult
{
    /// <summary>
    /// Makes a page from the items a repository read with <see cref="QueryableExtensions.ApplyPaging"/>:
    /// one more than the page size, so an extra item means there is a next page.
    /// </summary>
    public static PagedResult<T> FromWindow<T>(IReadOnlyList<T> window, ListOptions<T> options) where T : Entity
    {
        if (window.Count <= options.PageSize)
        {
            return PagedResult<T>.Create(window, nextCursor: null);
        }

        var items = window.Take(options.PageSize).ToList();
        return PagedResult<T>.Create(items, PageCursor.Encode(items[^1], options.Sorts));
    }
}
