using Padel.Api.Http;

namespace Padel.Api.Features.Members;

internal static class MembersGroup
{
    /// <summary>The <c>/api/members</c> route group that the members endpoints map onto.</summary>
    public static RouteGroupBuilder MapMembersGroup(this IEndpointRouteBuilder routes) =>
        routes.MapFeatureGroup("/api/members", "Members");
}
