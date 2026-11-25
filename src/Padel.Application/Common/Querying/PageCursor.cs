using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Padel.Domain.Common;

namespace Padel.Application.Common.Querying;

/// <summary>Where the next page starts: the sort keys and id of the last item of the previous page.</summary>
public sealed record CursorPosition(IReadOnlyList<object> Keys, Guid Id);

/// <summary>
/// The opaque <c>cursor</c> of a list: base64url JSON with the sort order and the position after which the
/// next page starts. Clients pass it back as is. A cursor only fits the sort order it was made for.
/// </summary>
public static class PageCursor
{
    public const int MaxLength = 1000;

    public static string Encode<T>(T lastItem, IReadOnlyList<SortExpression<T>> sorts) where T : Entity
    {
        var keys = sorts
            .Select(sort => sort.Field.GetValue(lastItem)
                ?? throw new InvalidOperationException($"Sort field '{sort.Field.Name}' is null; sort keys must have a value."))
            .Select(key => JsonSerializer.SerializeToElement(key, key.GetType()))
            .ToList();

        var payload = new Payload(SortOrder(sorts), keys, lastItem.Id);
        return Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(payload));
    }

    /// <summary>False when the cursor is malformed or was made for another sort order.</summary>
    public static bool TryDecode<T>(string cursor, IReadOnlyList<SortExpression<T>> sorts, [NotNullWhen(true)] out CursorPosition? position)
    {
        position = null;

        try
        {
            var payload = JsonSerializer.Deserialize<Payload>(Base64Url.DecodeFromChars(cursor));
            if (payload?.Keys is null || payload.Sort != SortOrder(sorts) || payload.Keys.Count != sorts.Count)
            {
                return false;
            }

            var keys = new List<object>(sorts.Count);
            for (var index = 0; index < sorts.Count; index++)
            {
                if (payload.Keys[index].Deserialize(sorts[index].Field.KeyType) is not { } key)
                {
                    return false;
                }

                keys.Add(key);
            }

            position = new CursorPosition(keys, payload.Id);
            return true;
        }
        catch (Exception exception) when (exception is FormatException or JsonException or NotSupportedException)
        {
            return false;
        }
    }

    private static string SortOrder<T>(IReadOnlyList<SortExpression<T>> sorts) => string.Join(',', sorts);

    private sealed record Payload(
        [property: JsonPropertyName("s")] string Sort,
        [property: JsonPropertyName("k")] IReadOnlyList<JsonElement> Keys,
        [property: JsonPropertyName("id")] Guid Id);
}
