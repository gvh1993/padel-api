using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Padel.Application.Common.Querying;

namespace Padel.Api.Http;

internal static class EndpointConventions
{
    private static readonly string[] ProblemContentTypes = [ContentNegotiation.ProblemJson, ContentNegotiation.ProblemXml];

    /// <summary>
    /// A route group for one feature: JSON or XML responses, 406 for other formats, and the shared error responses.
    /// </summary>
    public static RouteGroupBuilder MapFeatureGroup(this IEndpointRouteBuilder routes, string prefix, string tag)
    {
        var group = routes.MapGroup(prefix).WithTags(tag);

        group.AddEndpointFilter(async (context, next) =>
            ContentNegotiation.Select(context.HttpContext.Request) is null
                ? TypedResults.Problem(
                    statusCode: StatusCodes.Status406NotAcceptable,
                    detail: $"Supported response formats are {ContentNegotiation.Json} and {ContentNegotiation.Xml}.")
                : await next(context));

        group.WithMetadata(
            new ProducesResponseTypeMetadata(StatusCodes.Status401Unauthorized, typeof(ProblemDetails), ProblemContentTypes),
            new ProducesResponseTypeMetadata(StatusCodes.Status403Forbidden, typeof(ProblemDetails), ProblemContentTypes),
            new ProducesResponseTypeMetadata(StatusCodes.Status406NotAcceptable, typeof(ProblemDetails), ProblemContentTypes));

        return group;
    }

    public static RouteHandlerBuilder ProducesNegotiated<T>(this RouteHandlerBuilder builder, int statusCode = StatusCodes.Status200OK) =>
        builder.Produces<T>(statusCode, ContentNegotiation.Json, ContentNegotiation.Xml);

    public static RouteHandlerBuilder ProducesProblems(this RouteHandlerBuilder builder, params int[] statusCodes)
    {
        foreach (var statusCode in statusCodes)
        {
            builder.Produces<ProblemDetails>(statusCode, ContentNegotiation.ProblemJson, ContentNegotiation.ProblemXml);
        }

        return builder;
    }

    public static RouteHandlerBuilder ProducesValidationProblems(this RouteHandlerBuilder builder) =>
        builder.Produces<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentNegotiation.ProblemJson, ContentNegotiation.ProblemXml);
}

/// <summary>Paging and sorting query parameters shared by every list endpoint.</summary>
internal readonly record struct Paging(
    [Description("1-based page number.")]
    [FromQuery(Name = "page")] int Page = 1,
    [Description("Items per page (1-100).")]
    [FromQuery(Name = "pageSize")] int PageSize = ListQuery.DefaultPageSize,
    [Description("Comma separated sort fields; prefix a field with '-' to sort descending, e.g. `lastName,-registeredOn`.")]
    [FromQuery(Name = "sort")] string? Sort = null);
