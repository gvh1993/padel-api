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

    private static async Task<CreatedAtRoute<RegisterMemberResponse>> HandleAsync(
        RegisterMemberRequest request,
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

        var member = await dispatcher.Send<RegisterMemberCommand, MemberDto>(command, cancellationToken);
        return TypedResults.CreatedAtRoute(RegisterMemberResponse.From(member), GetMemberEndpoint.Name, new { id = member.Id });
    }
}
