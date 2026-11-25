using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Padel.Api.Authentication;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Features.Members;

namespace Padel.Api.Features.Members;

internal sealed class ReactivateMemberEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapMembersGroup()
            .MapPost("/{id:guid}/reactivate", HandleAsync)
            .RequireAuthorization(PadelAuthorization.AdminPolicy)
            .WithName("ReactivateMember")
            .WithSummary("Reactivates a suspended member.")
            .WithDescription("Admin only.")
            .ProducesProblems(StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed);

    private static async Task<Results<Ok<ReactivateMemberResponse>, ProblemHttpResult>> HandleAsync(Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        HttpResponse response, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
        (await dispatcher.Send<ReactivateMemberCommand, MemberDto>(new ReactivateMemberCommand(id, ETags.ParseIfMatch(ifMatch)), cancellationToken))
            .ToHttpResult(member => response.WithETag(member.Version, TypedResults.Ok(ReactivateMemberResponse.From(member))));
}
