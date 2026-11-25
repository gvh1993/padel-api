using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Padel.Api.Authentication;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Features.Bookings;

namespace Padel.Api.Features.Bookings;

internal sealed class MarkBookingNoShowEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapBookingsGroup()
            .MapPost("/{id:guid}/no-show", HandleAsync)
            .RequireAuthorization(PadelAuthorization.AdminPolicy)
            .WithName("MarkBookingNoShow")
            .WithSummary("Marks a booking as a no-show.")
            .WithDescription("Admin only. Only after the slot has started.")
            .ProducesProblems(StatusCodes.Status404NotFound, StatusCodes.Status422UnprocessableEntity, StatusCodes.Status412PreconditionFailed);

    private static async Task<Results<Ok<MarkBookingNoShowResponse>, ProblemHttpResult>> HandleAsync(Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        HttpResponse response, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
        (await dispatcher.Send<MarkBookingNoShowCommand, BookingDto>(new MarkBookingNoShowCommand(id, ETags.ParseIfMatch(ifMatch)), cancellationToken))
            .ToHttpResult(booking => response.WithETag(booking.Version, TypedResults.Ok(MarkBookingNoShowResponse.From(booking))));
}
