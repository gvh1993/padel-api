using Microsoft.AspNetCore.Http.HttpResults;
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
            .ProducesProblems(StatusCodes.Status404NotFound);

    private static async Task<Ok<SuspendMemberResponse>> HandleAsync(
        Guid id,
        SuspendMemberRequest? request,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(SuspendMemberResponse.From(
            await dispatcher.Send<SuspendMemberCommand, MemberDto>(new SuspendMemberCommand(id, request?.Reason), cancellationToken)));
}
