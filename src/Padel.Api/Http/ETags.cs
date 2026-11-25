namespace Padel.Api.Http;

/// <summary>
/// Entity tags from the row version: <c>"42"</c>. A client sends the tag back in <c>If-Match</c>;
/// the change only goes through when the resource still has that version (else 412).
/// </summary>
internal static class ETags
{
    public static string Format(uint version) => $"\"{version}\"";

    /// <summary>
    /// The version in an <c>If-Match</c> header: <c>null</c> when absent or <c>*</c> (no check).
    /// A value that is not one of our tags can never match, so it fails with 412.
    /// </summary>
    public static uint? ParseIfMatch(string? ifMatch)
    {
        if (string.IsNullOrWhiteSpace(ifMatch) || ifMatch.Trim() == "*")
        {
            return null;
        }

        var tag = ifMatch.Trim();
        if (tag.StartsWith("W/", StringComparison.Ordinal))
        {
            tag = tag[2..];
        }

        return tag.Length > 2 && tag[0] == '"' && tag[^1] == '"' && uint.TryParse(tag[1..^1], out var version) && version != NoVersion
            ? version
            : NoVersion;
    }

    /// <summary>Row versions are PostgreSQL transaction ids, which are never 0.</summary>
    private const uint NoVersion = 0;

    /// <summary>Sets the <c>ETag</c> header and returns the result unchanged.</summary>
    public static TResult WithETag<TResult>(this HttpResponse response, uint version, TResult result)
    {
        response.Headers.ETag = Format(version);
        return result;
    }
}
