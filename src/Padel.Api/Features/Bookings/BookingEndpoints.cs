using System.ComponentModel;
using Padel.Api.Authentication;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Common.Querying;
using Padel.Application.Features.Bookings;
using Padel.Domain.Bookings;

namespace Padel.Api.Features.Bookings;

internal static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder routes)
    {
        var bookings = routes.MapFeatureGroup("/api/bookings", "Bookings");

        bookings.MapPost("/", async (CreateBookingCommand command, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
            {
                var booking = await dispatcher.Send<CreateBookingCommand, BookingDto>(command, cancellationToken);
                return Negotiated.CreatedAtRoute("GetBooking", new { id = booking.Id }, booking);
            })
            .WithName("CreateBooking")
            .WithSummary("Books a court.")
            .WithDescription(
                "Members can only book for themselves; admins for anyone. The member must be active, " +
                "the court available, the slot in the future, 30 minutes to 3 hours long and free.")
            .ProducesNegotiated<BookingDto>(StatusCodes.Status201Created)
            .ProducesValidationProblems()
            .ProducesProblems(StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status422UnprocessableEntity);

        bookings.MapGet("/", async (
                [AsParameters] Paging paging,
                [Description("Only bookings of this member. Ignored for members, who only see their own bookings.")] Guid? memberId,
                [Description("Only bookings on this court.")] Guid? courtId,
                [Description("Only bookings with this status.")] BookingStatus? status,
                [Description("Only bookings that end after this moment.")] DateTimeOffset? from,
                [Description("Only bookings that start before this moment.")] DateTimeOffset? to,
                IRequestDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var query = new ListBookingsQuery
                {
                    Page = paging.Page,
                    PageSize = paging.PageSize,
                    Sort = paging.Sort,
                    MemberId = memberId,
                    CourtId = courtId,
                    Status = status,
                    From = from,
                    To = to,
                };

                return Negotiated.Ok(await dispatcher.Send<ListBookingsQuery, PagedResult<BookingDto>>(query, cancellationToken));
            })
            .WithName("ListBookings")
            .WithSummary("Lists bookings with paging, sorting and filtering.")
            .WithDescription("Admins see all bookings; members only their own. Sort fields: start, end, status, createdOn.")
            .ProducesNegotiated<PagedResult<BookingDto>>()
            .ProducesValidationProblems();

        bookings.MapGet("/{id:guid}", async (Guid id, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
                Negotiated.Ok(await dispatcher.Send<GetBookingQuery, BookingDto>(new GetBookingQuery(id), cancellationToken)))
            .WithName("GetBooking")
            .WithSummary("Gets a booking.")
            .WithDescription("Admins, or the member who made the booking.")
            .ProducesNegotiated<BookingDto>()
            .ProducesProblems(StatusCodes.Status404NotFound);

        bookings.MapPost("/{id:guid}/cancel", async (Guid id, CancelBookingRequest? request, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
                Negotiated.Ok(await dispatcher.Send<CancelBookingCommand, BookingDto>(new CancelBookingCommand(id, request?.Reason), cancellationToken)))
            .WithName("CancelBooking")
            .WithSummary("Cancels a booking.")
            .WithDescription("Admins, or the member who made the booking.")
            .ProducesNegotiated<BookingDto>()
            .ProducesValidationProblems()
            .ProducesProblems(StatusCodes.Status404NotFound, StatusCodes.Status422UnprocessableEntity);

        bookings.MapPost("/{id:guid}/complete", async (Guid id, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
                Negotiated.Ok(await dispatcher.Send<CompleteBookingCommand, BookingDto>(new CompleteBookingCommand(id), cancellationToken)))
            .RequireAuthorization(PadelAuthorization.AdminPolicy)
            .WithName("CompleteBooking")
            .WithSummary("Marks a booking as played.")
            .WithDescription("Admin only. Only after the slot has started.")
            .ProducesNegotiated<BookingDto>()
            .ProducesProblems(StatusCodes.Status404NotFound, StatusCodes.Status422UnprocessableEntity);

        bookings.MapPost("/{id:guid}/no-show", async (Guid id, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
                Negotiated.Ok(await dispatcher.Send<MarkBookingNoShowCommand, BookingDto>(new MarkBookingNoShowCommand(id), cancellationToken)))
            .RequireAuthorization(PadelAuthorization.AdminPolicy)
            .WithName("MarkBookingNoShow")
            .WithSummary("Marks a booking as a no-show.")
            .WithDescription("Admin only. Only after the slot has started.")
            .ProducesNegotiated<BookingDto>()
            .ProducesProblems(StatusCodes.Status404NotFound, StatusCodes.Status422UnprocessableEntity);

        return routes;
    }
}
