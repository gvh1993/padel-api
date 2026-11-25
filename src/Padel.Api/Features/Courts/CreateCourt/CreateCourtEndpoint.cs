using Microsoft.AspNetCore.Http.HttpResults;
using Padel.Api.Authentication;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Features.Courts;

namespace Padel.Api.Features.Courts;

internal sealed class CreateCourtEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapCourtsGroup()
            .MapPost("/", HandleAsync)
            .RequireAuthorization(PadelAuthorization.AdminPolicy)
            .WithName("CreateCourt")
            .WithSummary("Creates a court.")
            .WithDescription("Admin only.")
            .ProducesValidationProblems();

    private static async Task<Results<CreatedAtRoute<CreateCourtResponse>, ProblemHttpResult>> HandleAsync(
        CreateCourtRequest request,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var command = new CreateCourtCommand { Name = request.Name, Surface = request.Surface };

        var result = await dispatcher.Send<CreateCourtCommand, CourtDto>(command, cancellationToken);
        return result.ToHttpResult(court => TypedResults.CreatedAtRoute(CreateCourtResponse.From(court), GetCourtEndpoint.Name, new { id = court.Id }));
    }
}
