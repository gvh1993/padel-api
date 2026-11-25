namespace Padel.Application.Common.Querying;

/// <summary>
/// Base for list queries: cursor paging and sorting. Derived queries add their own filters.
/// </summary>
public abstract record ListQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>
    /// The <c>nextCursor</c> of the previous page; <c>null</c> for the first page.
    /// It must be used with the same sort order.
    /// </summary>
    public string? Cursor { get; init; }

    /// <summary>Number of items per page (1-100).</summary>
    public int PageSize { get; init; } = DefaultPageSize;

    /// <summary>
    /// Comma separated sort fields. Prefix a field with '-' for descending order,
    /// for example <c>lastName,-registeredOn</c>.
    /// </summary>
    public string? Sort { get; init; }
}
