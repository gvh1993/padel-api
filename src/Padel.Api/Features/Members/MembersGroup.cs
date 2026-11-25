using Padel.Api.Http;

namespace Padel.Api.Features.Members;

internal static class MembersGroup
{
    /// <summary>The <c>/api/v1/members</c> route group that the members endpoints map onto.</summary>
    public static RouteGroupBuilder MapMembersGroup(this IEndpointRouteBuilder routes) =>
        routes.MapFeatureGroup("/members", "Members");
}
