using Padel.Application.Common.Querying;

namespace Padel.Api.Http;

/// <summary>One page of a list. Every list endpoint derives its own response from it.</summary>
public abstract record PagedResponse<TItem>
{
    public IReadOnlyList<TItem> Items { get; init; } = [];

    /// <summary>
    /// Pass it as <c>cursor</c> (with the same <c>sort</c> and filters) to get the next page;
    /// <c>null</c> on the last page.
    /// </summary>
    public string? NextCursor { get; init; }
}

internal static class PagedResponse
{
    /// <summary>Maps a page of application results to an endpoint's list response.</summary>
    public static TResponse From<TResponse, TItem, TSource>(PagedResult<TSource> page, Func<TSource, TItem> map)
        where TResponse : PagedResponse<TItem>, new() => new()
        {
            Items = page.Items.Select(map).ToList(),
            NextCursor = page.NextCursor,
        };
}
