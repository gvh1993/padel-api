using Microsoft.AspNetCore.Http.HttpResults;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Common.Querying;
using Padel.Application.Features.Courts;

namespace Padel.Api.Features.Courts;

internal sealed class ListCourtsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapCourtsGroup()
            .MapGet("/", HandleAsync)
            .WithName("ListCourts")
            .WithSummary("Lists courts with paging, sorting and filtering.")
            .WithDescription("Sort fields: name, surface, status.")
            .ProducesValidationProblems();

    private static async Task<Ok<ListCourtsResponse>> HandleAsync(
        [AsParameters] ListCourtsRequest request,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var query = new ListCourtsQuery
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Sort = request.Sort,
            Status = request.Status,
            Surface = request.Surface,
            Search = request.Search,
        };

        var page = await dispatcher.Send<ListCourtsQuery, PagedResult<CourtDto>>(query, cancellationToken);
        return TypedResults.Ok(PagedResponse.From<ListCourtsResponse, ListCourtsItem, CourtDto>(page, ListCourtsItem.From));
    }
}
