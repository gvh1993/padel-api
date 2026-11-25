using Microsoft.AspNetCore.Http.HttpResults;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Common.Querying;
using Padel.Application.Features.Bookings;

namespace Padel.Api.Features.Bookings;

internal sealed class ListBookingsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapBookingsGroup()
            .MapGet("/", HandleAsync)
            .WithName("ListBookings")
            .WithSummary("Lists bookings with paging, sorting and filtering.")
            .WithDescription("Admins see all bookings; members only their own. Sort fields: start, end, status, createdOn.")
            .ProducesValidationProblems();

    private static async Task<Results<Ok<ListBookingsResponse>, ProblemHttpResult>> HandleAsync(
        [AsParameters] ListBookingsRequest request,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var query = new ListBookingsQuery
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Sort = request.Sort,
            MemberId = request.MemberId,
            CourtId = request.CourtId,
            Status = request.Status,
            From = request.From,
            To = request.To,
        };

        var result = await dispatcher.Send<ListBookingsQuery, PagedResult<BookingDto>>(query, cancellationToken);
        return result.ToHttpResult(page => TypedResults.Ok(PagedResponse.From<ListBookingsResponse, ListBookingsItem, BookingDto>(page, ListBookingsItem.From)));
    }
}
