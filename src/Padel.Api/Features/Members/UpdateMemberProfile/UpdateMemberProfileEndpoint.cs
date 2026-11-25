using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Features.Members;

namespace Padel.Api.Features.Members;

internal sealed class UpdateMemberProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapMembersGroup()
            .MapPut("/{id:guid}", HandleAsync)
            .WithName("UpdateMemberProfile")
            .WithSummary("Updates the name, email and phone of a member.")
            .WithDescription("Admins, or the member itself.")
            .ProducesValidationProblems()
            .ProducesProblems(StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed);

    private static async Task<Results<Ok<UpdateMemberProfileResponse>, ProblemHttpResult>> HandleAsync(
        Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        HttpResponse response,
        UpdateMemberProfileRequest request,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var command = new UpdateMemberProfileCommand
        {
            Id = id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            ExpectedVersion = ETags.ParseIfMatch(ifMatch),
        };

        return (await dispatcher.Send<UpdateMemberProfileCommand, MemberDto>(command, cancellationToken))
            .ToHttpResult(member => response.WithETag(member.Version, TypedResults.Ok(UpdateMemberProfileResponse.From(member))));
    }
}
