using Microsoft.AspNetCore.Http.HttpResults;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Features.Bookings;

namespace Padel.Api.Features.Bookings;

internal sealed class CreateBookingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapBookingsGroup()
            .MapPost("/", HandleAsync)
            .WithName("CreateBooking")
            .WithSummary("Books a court.")
            .WithDescription(
                "Members can only book for themselves; admins for anyone. The member must be active, " +
                "the court available, the slot in the future, 30 minutes to 3 hours long and free.")
            .ProducesValidationProblems()
            .ProducesProblems(StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status422UnprocessableEntity);

    private static async Task<Results<CreatedAtRoute<CreateBookingResponse>, ProblemHttpResult>> HandleAsync(
        CreateBookingRequest request,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var command = new CreateBookingCommand
        {
            MemberId = request.MemberId,
            CourtId = request.CourtId,
            Start = request.Start,
            End = request.End,
        };

        var result = await dispatcher.Send<CreateBookingCommand, BookingDto>(command, cancellationToken);
        return result.ToHttpResult(booking => TypedResults.CreatedAtRoute(CreateBookingResponse.From(booking), GetBookingEndpoint.Name, new { id = booking.Id }));
    }
}
