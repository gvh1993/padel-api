using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Padel.Api.Authentication;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Features.Courts;

namespace Padel.Api.Features.Courts;

internal sealed class RetireCourtEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapCourtsGroup()
            .MapPost("/{id:guid}/retire", HandleAsync)
            .RequireAuthorization(PadelAuthorization.AdminPolicy)
            .WithName("RetireCourt")
            .WithSummary("Retires a court permanently.")
            .WithDescription("Admin only. Retiring a retired court has no effect.")
            .ProducesProblems(StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed);

    private static async Task<Results<Ok<RetireCourtResponse>, ProblemHttpResult>> HandleAsync(Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        HttpResponse response, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
        (await dispatcher.Send<RetireCourtCommand, CourtDto>(new RetireCourtCommand(id, ETags.ParseIfMatch(ifMatch)), cancellationToken))
            .ToHttpResult(court => response.WithETag(court.Version, TypedResults.Ok(RetireCourtResponse.From(court))));
}
