using Microsoft.AspNetCore.Http.HttpResults;
using Padel.Api.Authentication;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Features.Bookings;

namespace Padel.Api.Features.Bookings;

internal sealed class CompleteBookingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapBookingsGroup()
            .MapPost("/{id:guid}/complete", HandleAsync)
            .RequireAuthorization(PadelAuthorization.AdminPolicy)
            .WithName("CompleteBooking")
            .WithSummary("Marks a booking as played.")
            .WithDescription("Admin only. Only after the slot has started.")
            .ProducesProblems(StatusCodes.Status404NotFound, StatusCodes.Status422UnprocessableEntity);

    private static async Task<Ok<CompleteBookingResponse>> HandleAsync(Guid id, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
        TypedResults.Ok(CompleteBookingResponse.From(
            await dispatcher.Send<CompleteBookingCommand, BookingDto>(new CompleteBookingCommand(id), cancellationToken)));
}
