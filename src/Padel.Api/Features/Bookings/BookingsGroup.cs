using Padel.Api.Http;

namespace Padel.Api.Features.Bookings;

internal static class BookingsGroup
{
    /// <summary>The <c>/api/v1/bookings</c> route group that the bookings endpoints map onto.</summary>
    public static RouteGroupBuilder MapBookingsGroup(this IEndpointRouteBuilder routes) =>
        routes.MapFeatureGroup("/bookings", "Bookings");
}
