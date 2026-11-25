using Microsoft.AspNetCore.Http.HttpResults;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Features.Bookings;

namespace Padel.Api.Features.Bookings;

internal sealed class GetBookingEndpoint : IEndpoint
{
    public const string Name = "GetBooking";

    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapBookingsGroup()
            .MapGet("/{id:guid}", HandleAsync)
            .WithName(Name)
            .WithSummary("Gets a booking.")
            .WithDescription("Admins, or the member who made the booking.")
            .ProducesProblems(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<GetBookingResponse>, ProblemHttpResult>> HandleAsync(Guid id, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
        (await dispatcher.Send<GetBookingQuery, BookingDto>(new GetBookingQuery(id), cancellationToken))
            .ToHttpResult(booking => TypedResults.Ok(GetBookingResponse.From(booking)));
}
