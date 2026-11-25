using Microsoft.AspNetCore.Http.HttpResults;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Features.Members;

namespace Padel.Api.Features.Members;

internal sealed class GetMemberEndpoint : IEndpoint
{
    public const string Name = "GetMember";

    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapMembersGroup()
            .MapGet("/{id:guid}", HandleAsync)
            .WithName(Name)
            .WithSummary("Gets a member.")
            .WithDescription("Admins, or the member itself.")
            .ProducesProblems(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<GetMemberResponse>, ProblemHttpResult>> HandleAsync(Guid id, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
        (await dispatcher.Send<GetMemberQuery, MemberDto>(new GetMemberQuery(id), cancellationToken))
            .ToHttpResult(member => TypedResults.Ok(GetMemberResponse.From(member)));
}
