using Microsoft.AspNetCore.Http.HttpResults;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Features.Bookings;

namespace Padel.Api.Features.Bookings;

internal sealed class CancelBookingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapBookingsGroup()
            .MapPost("/{id:guid}/cancel", HandleAsync)
            .WithName("CancelBooking")
            .WithSummary("Cancels a booking.")
            .WithDescription("Admins, or the member who made the booking.")
            .ProducesValidationProblems()
            .ProducesProblems(StatusCodes.Status404NotFound, StatusCodes.Status422UnprocessableEntity);

    private static async Task<Results<Ok<CancelBookingResponse>, ProblemHttpResult>> HandleAsync(
        Guid id,
        CancelBookingRequest? request,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken) =>
        (await dispatcher.Send<CancelBookingCommand, BookingDto>(new CancelBookingCommand(id, request?.Reason), cancellationToken))
            .ToHttpResult(booking => TypedResults.Ok(CancelBookingResponse.From(booking)));
}
