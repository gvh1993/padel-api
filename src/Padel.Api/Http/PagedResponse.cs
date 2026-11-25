using Padel.Application.Common.Querying;

namespace Padel.Api.Http;

/// <summary>One page of a list. Every list endpoint derives its own response from it.</summary>
public abstract record PagedResponse<TItem>
{
    public IReadOnlyList<TItem> Items { get; init; } = [];

    /// <summary>1-based page number.</summary>
    public int Page { get; init; }

    public int PageSize { get; init; }

    /// <summary>Number of items over all pages.</summary>
    public int TotalCount { get; init; }

    public int TotalPages { get; init; }
}

internal static class PagedResponse
{
    /// <summary>Maps a page of application results to an endpoint's list response.</summary>
    public static TResponse From<TResponse, TItem, TSource>(PagedResult<TSource> page, Func<TSource, TItem> map)
        where TResponse : PagedResponse<TItem>, new() => new()
        {
            Items = page.Items.Select(map).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
            TotalPages = page.TotalPages,
        };
}
