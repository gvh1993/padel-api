using System.ComponentModel;
using Padel.Api.Authentication;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Common.Querying;
using Padel.Application.Features.Bookings;
using Padel.Application.Features.Courts;
using Padel.Domain.Bookings;
using Padel.Domain.Courts;

namespace Padel.Api.Features.Courts;

internal static class CourtEndpoints
{
    public static IEndpointRouteBuilder MapCourtEndpoints(this IEndpointRouteBuilder routes)
    {
        var courts = routes.MapFeatureGroup("/api/courts", "Courts");

        courts.MapPost("/", async (CreateCourtCommand command, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
            {
                var court = await dispatcher.Send<CreateCourtCommand, CourtDto>(command, cancellationToken);
                return TypedResults.CreatedAtRoute(court, "GetCourt", new { id = court.Id });
            })
            .RequireAuthorization(PadelAuthorization.AdminPolicy)
            .WithName("CreateCourt")
            .WithSummary("Creates a court.")
            .WithDescription("Admin only.")
            .ProducesValidationProblems();

        courts.MapGet("/", async (
                [AsParameters] Paging paging,
                [Description("Only courts with this status.")] CourtStatus? status,
                [Description("Case-insensitive exact match on the surface, e.g. `glass`.")] string? surface,
                [Description("Case-insensitive match on the court name.")] string? search,
                IRequestDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var query = new ListCourtsQuery
                {
                    Page = paging.Page,
                    PageSize = paging.PageSize,
                    Sort = paging.Sort,
                    Status = status,
                    Surface = surface,
                    Search = search,
                };

                return TypedResults.Ok(await dispatcher.Send<ListCourtsQuery, PagedResult<CourtDto>>(query, cancellationToken));
            })
            .WithName("ListCourts")
            .WithSummary("Lists courts with paging, sorting and filtering.")
            .WithDescription("Sort fields: name, surface, status.")
            .ProducesValidationProblems();

        courts.MapGet("/{id:guid}", async (Guid id, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
                TypedResults.Ok(await dispatcher.Send<GetCourtQuery, CourtDto>(new GetCourtQuery(id), cancellationToken)))
            .WithName("GetCourt")
            .WithSummary("Gets a court.")
            .ProducesProblems(StatusCodes.Status404NotFound);

        courts.MapPut("/{id:guid}/name", async (Guid id, RenameCourtRequest request, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
                TypedResults.Ok(await dispatcher.Send<RenameCourtCommand, CourtDto>(new RenameCourtCommand(id, request.Name), cancellationToken)))
            .RequireAuthorization(PadelAuthorization.AdminPolicy)
            .WithName("RenameCourt")
            .WithSummary("Renames a court.")
            .WithDescription("Admin only.")
            .ProducesValidationProblems()
            .ProducesProblems(StatusCodes.Status404NotFound);

        courts.MapPut("/{id:guid}/maintenance", async (Guid id, SetCourtMaintenanceRequest request, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
                TypedResults.Ok(await dispatcher.Send<SetCourtMaintenanceCommand, CourtDto>(
                    new SetCourtMaintenanceCommand(id, request.UnderMaintenance), cancellationToken)))
            .RequireAuthorization(PadelAuthorization.AdminPolicy)
            .WithName("SetCourtMaintenance")
            .WithSummary("Puts a court in or out of maintenance. Courts in maintenance cannot be booked.")
            .WithDescription("Admin only.")
            .ProducesProblems(StatusCodes.Status404NotFound, StatusCodes.Status422UnprocessableEntity);

        courts.MapPost("/{id:guid}/retire", async (Guid id, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
                TypedResults.Ok(await dispatcher.Send<RetireCourtCommand, CourtDto>(new RetireCourtCommand(id), cancellationToken)))
            .RequireAuthorization(PadelAuthorization.AdminPolicy)
            .WithName("RetireCourt")
            .WithSummary("Retires a court permanently.")
            .WithDescription("Admin only. Retiring a retired court has no effect.")
            .ProducesProblems(StatusCodes.Status404NotFound);

        courts.MapGet("/{id:guid}/bookings", async (
                Guid id,
                [AsParameters] Paging paging,
                [Description("Only bookings with this status.")] BookingStatus? status,
                [Description("Only bookings that end after this moment.")] DateTimeOffset? from,
                [Description("Only bookings that start before this moment.")] DateTimeOffset? to,
                IRequestDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var query = new ListCourtBookingsQuery
                {
                    CourtId = id,
                    Page = paging.Page,
                    PageSize = paging.PageSize,
                    Sort = paging.Sort,
                    Status = status,
                    From = from,
                    To = to,
                };

                return TypedResults.Ok(await dispatcher.Send<ListCourtBookingsQuery, PagedResult<BookingDto>>(query, cancellationToken));
            })
            .WithName("ListCourtBookings")
            .WithSummary("Lists the bookings (schedule) of a court.")
            .WithDescription("Sort fields: start, end, status, createdOn.")
            .ProducesValidationProblems()
            .ProducesProblems(StatusCodes.Status404NotFound);

        return routes;
    }
}
