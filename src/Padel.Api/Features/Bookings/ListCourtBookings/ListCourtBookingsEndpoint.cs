using Microsoft.AspNetCore.Http.HttpResults;
using Padel.Api.Features.Courts;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Common.Querying;
using Padel.Application.Features.Bookings;

namespace Padel.Api.Features.Bookings;

internal sealed class ListCourtBookingsEndpoint : IEndpoint
{
    // The schedule of a court lives under the court's route: /api/v1/courts/{id}/bookings.
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapCourtsGroup()
            .MapGet("/{id:guid}/bookings", HandleAsync)
            .WithName("ListCourtBookings")
            .WithSummary("Lists the bookings (schedule) of a court.")
            .WithDescription("Sort fields: start, end, status, createdOn.")
            .ProducesValidationProblems()
            .ProducesProblems(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<ListCourtBookingsResponse>, ProblemHttpResult>> HandleAsync(
        Guid id,
        [AsParameters] ListCourtBookingsRequest request,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var query = new ListCourtBookingsQuery
        {
            CourtId = id,
            Cursor = request.Cursor,
            PageSize = request.PageSize,
            Sort = request.Sort,
            Status = request.Status,
            From = request.From,
            To = request.To,
        };

        var result = await dispatcher.Send<ListCourtBookingsQuery, PagedResult<BookingDto>>(query, cancellationToken);
        return result.ToHttpResult(page => TypedResults.Ok(PagedResponse.From<ListCourtBookingsResponse, ListCourtBookingsItem, BookingDto>(page, ListCourtBookingsItem.From)));
    }
}
