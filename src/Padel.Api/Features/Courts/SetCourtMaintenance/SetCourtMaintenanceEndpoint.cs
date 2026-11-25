using Microsoft.AspNetCore.Http.HttpResults;
using Padel.Api.Authentication;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Features.Courts;

namespace Padel.Api.Features.Courts;

internal sealed class SetCourtMaintenanceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapCourtsGroup()
            .MapPut("/{id:guid}/maintenance", HandleAsync)
            .RequireAuthorization(PadelAuthorization.AdminPolicy)
            .WithName("SetCourtMaintenance")
            .WithSummary("Puts a court in or out of maintenance. Courts in maintenance cannot be booked.")
            .WithDescription("Admin only.")
            .ProducesProblems(StatusCodes.Status404NotFound, StatusCodes.Status422UnprocessableEntity);

    private static async Task<Results<Ok<SetCourtMaintenanceResponse>, ProblemHttpResult>> HandleAsync(
        Guid id,
        SetCourtMaintenanceRequest request,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken) =>
        (await dispatcher.Send<SetCourtMaintenanceCommand, CourtDto>(
            new SetCourtMaintenanceCommand(id, request.UnderMaintenance), cancellationToken))
            .ToHttpResult(court => TypedResults.Ok(SetCourtMaintenanceResponse.From(court)));
}
