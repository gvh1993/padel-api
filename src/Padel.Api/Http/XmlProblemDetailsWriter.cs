using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Padel.Api.Http;

/// <summary>
/// Writes problem details as <c>application/problem+xml</c> (RFC 9457, appendix B) for clients that prefer XML.
/// The built-in writer handles <c>application/problem+json</c>.
/// </summary>
internal sealed class XmlProblemDetailsWriter(IOptions<ProblemDetailsOptions> options) : IProblemDetailsWriter
{
    private static readonly XNamespace Rfc9457 = "urn:ietf:rfc:7807";

    public bool CanWrite(ProblemDetailsContext context) =>
        ContentNegotiation.Select(context.HttpContext.Request) == ResponseFormat.Xml;

    public async ValueTask WriteAsync(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        problem.Status ??= context.HttpContext.Response.StatusCode;
        problem.Title ??= ReasonPhrases.GetReasonPhrase(problem.Status.Value);
        problem.Type ??= ProblemTypes.For(problem.Status.Value);
        options.Value.CustomizeProblemDetails?.Invoke(context);

        var root = new XElement(Rfc9457 + "problem",
            Element("type", problem.Type),
            Element("title", problem.Title),
            Element("status", problem.Status),
            Element("detail", problem.Detail),
            Element("instance", problem.Instance));

        if (problem is HttpValidationProblemDetails validation)
        {
            root.Add(new XElement(Rfc9457 + "errors", validation.Errors.Select(error =>
                new XElement(Rfc9457 + XmlConvert.EncodeLocalName(error.Key), error.Value.Select(message => Element("i", message))))));
        }

        foreach (var (key, value) in problem.Extensions)
        {
            root.Add(Element(XmlConvert.EncodeLocalName(key), value is JsonElement json ? json.ToString() : value));
        }

        context.HttpContext.Response.ContentType = $"{ContentNegotiation.ProblemXml}; charset=utf-8";

        await using var writer = XmlWriter.Create(context.HttpContext.Response.Body, WriterSettings);
        await new XDocument(root).SaveAsync(writer, context.HttpContext.RequestAborted);
    }

    // UTF-8 without a byte order mark, like the other responses.
    private static readonly XmlWriterSettings WriterSettings = new() { Encoding = new UTF8Encoding(false), Async = true };

    private static XElement? Element(string name, object? value) =>
        value is null ? null : new XElement(Rfc9457 + name, value);
}

/// <summary>The same <c>type</c> URIs the built-in JSON writer uses.</summary>
internal static class ProblemTypes
{
    public static string For(int status) => status switch
    {
        400 => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        401 => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        403 => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        404 => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        405 => "https://tools.ietf.org/html/rfc9110#section-15.5.6",
        406 => "https://tools.ietf.org/html/rfc9110#section-15.5.7",
        409 => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        415 => "https://tools.ietf.org/html/rfc9110#section-15.5.16",
        422 => "https://tools.ietf.org/html/rfc9110#section-15.5.21",
        500 => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
        _ => "about:blank",
    };
}
