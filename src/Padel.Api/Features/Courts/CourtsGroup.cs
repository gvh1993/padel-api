using Padel.Api.Http;

namespace Padel.Api.Features.Courts;

internal static class CourtsGroup
{
    /// <summary>The <c>/api/courts</c> route group that the courts endpoints map onto.</summary>
    public static RouteGroupBuilder MapCourtsGroup(this IEndpointRouteBuilder routes) =>
        routes.MapFeatureGroup("/api/courts", "Courts");
}
