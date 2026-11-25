using Padel.Api.Http;

namespace Padel.Api.Features.Bookings;

internal static class BookingsGroup
{
    /// <summary>The <c>/api/bookings</c> route group that the bookings endpoints map onto.</summary>
    public static RouteGroupBuilder MapBookingsGroup(this IEndpointRouteBuilder routes) =>
        routes.MapFeatureGroup("/api/bookings", "Bookings");
}
