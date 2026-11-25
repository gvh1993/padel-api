using Microsoft.AspNetCore.Http.HttpResults;
using Padel.Api.Authentication;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Features.Members;

namespace Padel.Api.Features.Members;

internal sealed class RegisterMemberEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapMembersGroup()
            .MapPost("/", HandleAsync)
            .RequireAuthorization(PadelAuthorization.AdminPolicy)
            .WithName("RegisterMember")
            .WithSummary("Registers a new member.")
            .WithDescription("Admin only.")
            .ProducesValidationProblems()
            .ProducesProblems(StatusCodes.Status409Conflict);

    private static async Task<Results<CreatedAtRoute<RegisterMemberResponse>, ProblemHttpResult>> HandleAsync(
        RegisterMemberRequest request,
        HttpResponse response,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var command = new RegisterMemberCommand
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
        };

        var result = await dispatcher.Send<RegisterMemberCommand, MemberDto>(command, cancellationToken);
        return result.ToHttpResult(member => response.WithETag(member.Version, TypedResults.CreatedAtRoute(RegisterMemberResponse.From(member), GetMemberEndpoint.Name, new { id = member.Id })));
    }
}
