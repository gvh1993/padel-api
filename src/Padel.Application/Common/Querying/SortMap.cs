using System.Linq.Expressions;

namespace Padel.Application.Common.Querying;

public sealed record SortExpression<T>(Expression<Func<T, object?>> KeySelector, bool Descending);

/// <summary>
/// Whitelist of sortable fields for an entity, mapping public field names to key selectors.
/// </summary>
public sealed class SortMap<T>(string defaultSort)
{
    private readonly Dictionary<string, Expression<Func<T, object?>>> _fields = new(StringComparer.OrdinalIgnoreCase);

    public string DefaultSort { get; } = defaultSort;

    public IEnumerable<string> FieldNames => _fields.Keys;

    public SortMap<T> Add(string name, Expression<Func<T, object?>> keySelector)
    {
        _fields.Add(name, keySelector);
        return this;
    }

    public IReadOnlyList<string> FindUnknownFields(string? sort) =>
        Parse(sort).Where(part => !_fields.ContainsKey(part.Field)).Select(part => part.Field).ToList();

    public IReadOnlyList<SortExpression<T>> Resolve(string? sort)
    {
        var parts = Parse(string.IsNullOrWhiteSpace(sort) ? DefaultSort : sort);

        return parts
            .DistinctBy(part => part.Field, StringComparer.OrdinalIgnoreCase)
            .Select(part => _fields.TryGetValue(part.Field, out var keySelector)
                ? new SortExpression<T>(keySelector, part.Descending)
                // The request validator rejects unknown fields (FindUnknownFields), so this is a programming error.
                : throw new InvalidOperationException($"Unknown sort field '{part.Field}'. Validate the sort with FindUnknownFields first."))
            .ToList();
    }

    private static IEnumerable<(string Field, bool Descending)> Parse(string? sort) =>
        (sort ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.StartsWith('-')
                ? (part[1..].Trim(), true)
                : (part.TrimStart('+').Trim(), false));
}
