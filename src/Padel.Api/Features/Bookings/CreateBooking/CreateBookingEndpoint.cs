using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Features.Bookings;

namespace Padel.Api.Features.Bookings;

internal sealed class CreateBookingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapBookingsGroup()
            .MapPost("/", HandleAsync)
            .RequireRateLimiting(RateLimiting.CreateBookingPolicy)
            .WithName("CreateBooking")
            .WithSummary("Books a court.")
            .WithDescription(
                "Members can only book for themselves; admins for anyone. The member must be active and may have " +
                "a limited number of upcoming bookings; the court must be available and free; the slot must be in the " +
                "future, within opening hours, on the half hour and 30 minutes to 3 hours long. Send an " +
                "`Idempotency-Key` header to retry safely: a retry with the same key returns the same booking.")
            .ProducesValidationProblems()
            .ProducesProblems(StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status422UnprocessableEntity);

    private static async Task<Results<CreatedAtRoute<CreateBookingResponse>, ProblemHttpResult>> HandleAsync(
        CreateBookingRequest request,
        HttpResponse response,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var command = new CreateBookingCommand
        {
            MemberId = request.MemberId,
            CourtId = request.CourtId,
            Start = request.Start,
            End = request.End,
            IdempotencyKey = idempotencyKey,
        };

        var result = await dispatcher.Send<CreateBookingCommand, BookingDto>(command, cancellationToken);
        return result.ToHttpResult(booking => response.WithETag(booking.Version, TypedResults.CreatedAtRoute(CreateBookingResponse.From(booking), GetBookingEndpoint.Name, new { id = booking.Id })));
    }
}
