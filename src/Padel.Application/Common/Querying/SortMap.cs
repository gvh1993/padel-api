using System.Linq.Expressions;

namespace Padel.Application.Common.Querying;

/// <summary>
/// A sortable field: its public name and its key, typed so the cursor can compare on it.
/// </summary>
public sealed class SortField<T>
{
    internal SortField(string name, LambdaExpression key)
    {
        Name = name;
        Key = key;
        OrderKey = Expression.Lambda<Func<T, object?>>(Expression.Convert(key.Body, typeof(object)), key.Parameters);
        GetValue = OrderKey.Compile();
    }

    public string Name { get; }

    /// <summary>The key, e.g. <c>member =&gt; member.Name.Last</c>; <see cref="KeyType"/> is its type.</summary>
    public LambdaExpression Key { get; }

    public Type KeyType => Key.ReturnType;

    /// <summary>The key boxed to <see cref="object"/>, for <c>OrderBy</c>.</summary>
    public Expression<Func<T, object?>> OrderKey { get; }

    /// <summary>Reads the key of an item, to put it in the cursor.</summary>
    public Func<T, object?> GetValue { get; }
}

public sealed record SortExpression<T>(SortField<T> Field, bool Descending)
{
    public override string ToString() => Descending ? $"-{Field.Name}" : Field.Name;
}

/// <summary>
/// Whitelist of sortable fields for an entity, mapping public field names to keys.
/// Keys must not be null: the cursor compares on them.
/// </summary>
public sealed class SortMap<T>(string defaultSort)
{
    private readonly Dictionary<string, SortField<T>> _fields = new(StringComparer.OrdinalIgnoreCase);

    public string DefaultSort { get; } = defaultSort;

    public IEnumerable<string> FieldNames => _fields.Values.Select(sortField => sortField.Name);

    /// <summary>
    /// Adds a sortable field. Enums sort by their numeric value (not by their stored text), so the
    /// cursor can compare them as numbers in the database and in memory alike.
    /// </summary>
    public SortMap<T> Add<TKey>(string name, Expression<Func<T, TKey>> key)
    {
        LambdaExpression typedKey = typeof(TKey).IsEnum ? ToValueOrder(key) : key;
        _fields.Add(name, new SortField<T>(name, typedKey));
        return this;
    }

    public IReadOnlyList<string> FindUnknownFields(string? sort) =>
        Parse(sort).Where(part => !_fields.ContainsKey(part.Field)).Select(part => part.Field).ToList();

    public IReadOnlyList<SortExpression<T>> Resolve(string? sort)
    {
        var parts = Parse(string.IsNullOrWhiteSpace(sort) ? DefaultSort : sort);

        return parts
            .DistinctBy(part => part.Field, StringComparer.OrdinalIgnoreCase)
            .Select(part => _fields.TryGetValue(part.Field, out var field)
                ? new SortExpression<T>(field, part.Descending)
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

    /// <summary>
    /// <c>status</c> becomes <c>status == A ? 0 : status == B ? 1 : 2</c> (a SQL <c>CASE</c>): enums are
    /// stored as text, and comparing that text would not match the order of the enum in memory.
    /// </summary>
    private static Expression<Func<T, int>> ToValueOrder<TKey>(Expression<Func<T, TKey>> key)
    {
        var values = Enum.GetValues(typeof(TKey)).Cast<object>().ToList();

        Expression position = Expression.Constant(values.Count);
        for (var index = values.Count - 1; index >= 0; index--)
        {
            position = Expression.Condition(
                Expression.Equal(key.Body, Expression.Constant(values[index], typeof(TKey))),
                Expression.Constant(index),
                position);
        }

        return Expression.Lambda<Func<T, int>>(position, key.Parameters);
    }
}
