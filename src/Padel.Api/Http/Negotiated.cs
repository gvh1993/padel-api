using System.Collections.Concurrent;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Padel.Api.Http;

/// <summary>
/// A result that is written as JSON or XML, depending on the <c>Accept</c> header.
/// Minimal APIs only write JSON themselves.
/// </summary>
internal sealed class Negotiated<T>(T value, int statusCode, CreatedAt? createdAt = null) : IResult, IStatusCodeHttpResult, IValueHttpResult, IValueHttpResult<T>
{
    private static readonly XmlWriterSettings XmlSettings = new() { Encoding = new UTF8Encoding(false) };

    public T Value => value;

    object? IValueHttpResult.Value => value;

    public int? StatusCode => statusCode;

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        if (createdAt is not null)
        {
            var links = httpContext.RequestServices.GetRequiredService<LinkGenerator>();
            httpContext.Response.Headers.Location = links.GetUriByName(httpContext, createdAt.RouteName, createdAt.RouteValues);
        }

        if (ContentNegotiation.Select(httpContext.Request) != ResponseFormat.Xml)
        {
            await TypedResults.Json(value, statusCode: statusCode).ExecuteAsync(httpContext);
            return;
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = $"{ContentNegotiation.Xml}; charset=utf-8";

        // XmlSerializer writes synchronously and Kestrel only allows asynchronous writes, so buffer first.
        using var buffer = new MemoryStream();
        using (var writer = XmlWriter.Create(buffer, XmlSettings))
        {
            XmlSerializers.For(typeof(T)).Serialize(writer, value);
        }

        buffer.Position = 0;
        await buffer.CopyToAsync(httpContext.Response.Body, httpContext.RequestAborted);
    }
}

internal static class Negotiated
{
    public static Negotiated<T> Ok<T>(T value) => new(value, StatusCodes.Status200OK);

    /// <summary>201 Created, with an absolute <c>Location</c> to the named route that gets the resource.</summary>
    public static Negotiated<T> CreatedAtRoute<T>(string routeName, object routeValues, T value) =>
        new(value, StatusCodes.Status201Created, new CreatedAt(routeName, routeValues));
}

internal sealed record CreatedAt(string RouteName, object RouteValues);

internal static class XmlSerializers
{
    // Creating an XmlSerializer generates code; cache one per type.
    private static readonly ConcurrentDictionary<Type, XmlSerializer> Cache = new();

    public static XmlSerializer For(Type type) => Cache.GetOrAdd(type, static type => new XmlSerializer(type));
}
