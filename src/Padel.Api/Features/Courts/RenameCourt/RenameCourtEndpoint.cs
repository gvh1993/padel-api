using Microsoft.AspNetCore.Http.HttpResults;
using Padel.Api.Authentication;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Features.Courts;

namespace Padel.Api.Features.Courts;

internal sealed class RenameCourtEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapCourtsGroup()
            .MapPut("/{id:guid}/name", HandleAsync)
            .RequireAuthorization(PadelAuthorization.AdminPolicy)
            .WithName("RenameCourt")
            .WithSummary("Renames a court.")
            .WithDescription("Admin only.")
            .ProducesValidationProblems()
            .ProducesProblems(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<RenameCourtResponse>, ProblemHttpResult>> HandleAsync(
        Guid id,
        RenameCourtRequest request,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken) =>
        (await dispatcher.Send<RenameCourtCommand, CourtDto>(new RenameCourtCommand(id, request.Name), cancellationToken))
            .ToHttpResult(court => TypedResults.Ok(RenameCourtResponse.From(court)));
}
