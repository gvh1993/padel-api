using Padel.Api.Http;
using Padel.Application.Features.Courts;
using Padel.Domain.Courts;

namespace Padel.Api.Features.Courts;

/// <summary>One page of courts.</summary>
public sealed record ListCourtsResponse : PagedResponse<ListCourtsItem>;

/// <summary>A court in the list.</summary>
public sealed record ListCourtsItem(
    Guid Id,
    string Name,
    string Surface,
    CourtStatus Status)
{
    internal static ListCourtsItem From(CourtDto court) => new(
        court.Id,
        court.Name,
        court.Surface,
        court.Status);
}
