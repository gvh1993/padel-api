namespace Padel.Application.Common.Querying;

/// <summary>
/// One page of results. Uses a concrete list and settable properties so it serializes to both JSON and XML.
/// </summary>
public sealed record PagedResult<T>
{
    public List<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }

    public static PagedResult<T> Create(IEnumerable<T> items, int page, int pageSize, int totalCount) => new()
    {
        Items = items.ToList(),
        Page = page,
        PageSize = pageSize,
        TotalCount = totalCount,
        TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize),
    };

    public PagedResult<TOut> Map<TOut>(Func<T, TOut> map) =>
        PagedResult<TOut>.Create(Items.Select(map), Page, PageSize, TotalCount);
}
