using Microsoft.Net.Http.Headers;

namespace Padel.Api.Http;

internal enum ResponseFormat
{
    Json,
    Xml,
}

/// <summary>
/// Picks the response format from the <c>Accept</c> header: JSON by default, XML when the client prefers it.
/// </summary>
internal static class ContentNegotiation
{
    public const string Json = "application/json";
    public const string Xml = "application/xml";
    public const string ProblemJson = "application/problem+json";
    public const string ProblemXml = "application/problem+xml";

    /// <summary>Returns the preferred supported format, or <c>null</c> when the client accepts neither.</summary>
    public static ResponseFormat? Select(HttpRequest request)
    {
        var accept = request.GetTypedHeaders().Accept;
        if (accept.Count == 0)
        {
            return ResponseFormat.Json;
        }

        // OrderByDescending is stable, so equal qualities keep the client's order.
        foreach (var mediaType in accept.OrderByDescending(mediaType => mediaType.Quality ?? 1))
        {
            if (mediaType.Quality is 0)
            {
                continue;
            }

            if (Matches(mediaType, ResponseFormat.Json))
            {
                return ResponseFormat.Json;
            }

            if (Matches(mediaType, ResponseFormat.Xml))
            {
                return ResponseFormat.Xml;
            }
        }

        return null;
    }

    private static bool Matches(MediaTypeHeaderValue mediaType, ResponseFormat format)
    {
        var value = mediaType.MediaType.Value?.ToLowerInvariant();

        return format switch
        {
            ResponseFormat.Json => value is "*/*" or "application/*" or Json or ProblemJson or "text/json"
                || value?.EndsWith("+json", StringComparison.Ordinal) == true,
            ResponseFormat.Xml => value is Xml or ProblemXml or "text/xml"
                || value?.EndsWith("+xml", StringComparison.Ordinal) == true,
            _ => false,
        };
    }
}
