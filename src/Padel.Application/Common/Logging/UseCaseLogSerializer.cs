using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Padel.Application.Common.Querying;

namespace Padel.Application.Common.Logging;

/// <summary>
/// Writes requests and results as JSON for the use case log. A page of results is summarised
/// (count, ids, whether there is a next page), so a list of up to 100 items does not end up in every log entry.
/// </summary>
public static class UseCaseLogSerializer
{
    /// <summary>Longer JSON is cut off, so a large request cannot flood the log.</summary>
    public const int MaxLength = 4096;

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly ConcurrentDictionary<Type, PropertyInfo?> IdProperties = new();

    public static string Serialize(object? value)
    {
        var json = value switch
        {
            null => "null",
            IPagedResult page => JsonSerializer.Serialize(Summarise(page), Options),
            _ => JsonSerializer.Serialize(value, value.GetType(), Options),
        };

        return json.Length <= MaxLength ? json : $"{json[..MaxLength]}... ({json.Length} characters)";
    }

    private static PageSummary Summarise(IPagedResult page)
    {
        var ids = page.Items
            .Select(item => IdProperties.GetOrAdd(item.GetType(), type => type.GetProperty("Id"))?.GetValue(item))
            .OfType<Guid>()
            .ToList();

        return new PageSummary(page.Items.Count, ids, page.NextCursor is not null);
    }

    private sealed record PageSummary(int Count, IReadOnlyList<Guid> Ids, bool HasNextPage);
}
