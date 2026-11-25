using Microsoft.AspNetCore.Mvc;

namespace Padel.Api.Http;

internal static class EndpointConventions
{
    /// <summary>A route group for one feature, documenting the error responses every endpoint can return.</summary>
    public static RouteGroupBuilder MapFeatureGroup(this IEndpointRouteBuilder routes, string prefix, string tag)
    {
        var group = routes.MapGroup(prefix).WithTags(tag);

        group.WithMetadata(
            new ProducesResponseTypeMetadata(StatusCodes.Status401Unauthorized, typeof(ProblemDetails), ["application/problem+json"]),
            new ProducesResponseTypeMetadata(StatusCodes.Status403Forbidden, typeof(ProblemDetails), ["application/problem+json"]),
            new ProducesResponseTypeMetadata(StatusCodes.Status429TooManyRequests, typeof(ProblemDetails), ["application/problem+json"]));

        return group;
    }

    public static RouteHandlerBuilder ProducesProblems(this RouteHandlerBuilder builder, params int[] statusCodes)
    {
        foreach (var statusCode in statusCodes)
        {
            builder.ProducesProblem(statusCode);
        }

        return builder;
    }

    public static RouteHandlerBuilder ProducesValidationProblems(this RouteHandlerBuilder builder) =>
        builder.ProducesValidationProblem();
}

/// <summary>Descriptions of the paging and sorting parameters that every list request has.</summary>
internal static class QueryDescriptions
{
    public const string Cursor = "The `nextCursor` of the previous page; omit it for the first page. Use it with the same `sort`.";
    public const string PageSize = "Items per page (1-100).";
    public const string Sort = "Comma separated sort fields; prefix a field with '-' to sort descending, e.g. `lastName,-registeredOn`.";
}
