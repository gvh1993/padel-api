using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Padel.Api.Authentication;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Features.Members;

namespace Padel.Api.Features.Members;

internal sealed class SuspendMemberEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapMembersGroup()
            .MapPost("/{id:guid}/suspend", HandleAsync)
            .RequireAuthorization(PadelAuthorization.AdminPolicy)
            .WithName("SuspendMember")
            .WithSummary("Suspends a member. Suspended members cannot book.")
            .WithDescription("Admin only. Suspending a suspended member has no effect.")
            .ProducesValidationProblems()
            .ProducesProblems(StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed);

    private static async Task<Results<Ok<SuspendMemberResponse>, ProblemHttpResult>> HandleAsync(
        Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        HttpResponse response,
        SuspendMemberRequest? request,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken) =>
        (await dispatcher.Send<SuspendMemberCommand, MemberDto>(new SuspendMemberCommand(id, request?.Reason, ETags.ParseIfMatch(ifMatch)), cancellationToken))
            .ToHttpResult(member => response.WithETag(member.Version, TypedResults.Ok(SuspendMemberResponse.From(member))));
}
