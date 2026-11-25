using Microsoft.AspNetCore.Http.HttpResults;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Features.Courts;

namespace Padel.Api.Features.Courts;

internal sealed class GetCourtEndpoint : IEndpoint
{
    public const string Name = "GetCourt";

    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapCourtsGroup()
            .MapGet("/{id:guid}", HandleAsync)
            .WithName(Name)
            .WithSummary("Gets a court.")
            .ProducesProblems(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<GetCourtResponse>, ProblemHttpResult>> HandleAsync(Guid id, HttpResponse response, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
        (await dispatcher.Send<GetCourtQuery, CourtDto>(new GetCourtQuery(id), cancellationToken))
            .ToHttpResult(court => response.WithETag(court.Version, TypedResults.Ok(GetCourtResponse.From(court))));
}
