using Microsoft.AspNetCore.Http.HttpResults;
using Padel.Api.Authentication;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Common.Querying;
using Padel.Application.Features.Members;

namespace Padel.Api.Features.Members;

internal sealed class ListMembersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapMembersGroup()
            .MapGet("/", HandleAsync)
            .RequireAuthorization(PadelAuthorization.AdminPolicy)
            .WithName("ListMembers")
            .WithSummary("Lists members with paging, sorting and filtering.")
            .WithDescription("Admin only. Sort fields: firstName, lastName, email, status, registeredOn.")
            .ProducesValidationProblems();

    private static async Task<Ok<ListMembersResponse>> HandleAsync(
        [AsParameters] ListMembersRequest request,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var query = new ListMembersQuery
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Sort = request.Sort,
            Status = request.Status,
            Search = request.Search,
        };

        var page = await dispatcher.Send<ListMembersQuery, PagedResult<MemberDto>>(query, cancellationToken);
        return TypedResults.Ok(PagedResponse.From<ListMembersResponse, ListMembersItem, MemberDto>(page, ListMembersItem.From));
    }
}
