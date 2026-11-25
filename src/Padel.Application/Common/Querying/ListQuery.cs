namespace Padel.Application.Common.Querying;

/// <summary>
/// Base for list queries: paging and sorting. Derived queries add their own filters.
/// </summary>
public abstract record ListQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>1-based page number.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Number of items per page (1-100).</summary>
    public int PageSize { get; init; } = DefaultPageSize;

    /// <summary>
    /// Comma separated sort fields. Prefix a field with '-' for descending order,
    /// for example <c>lastName,-registeredOn</c>.
    /// </summary>
    public string? Sort { get; init; }
}
