using Padel.Application.Features.Courts;
using Padel.Domain.Courts;

namespace Padel.Api.Features.Courts;

/// <summary>A court.</summary>
public sealed record GetCourtResponse(
    Guid Id,
    string Name,
    string Surface,
    CourtStatus Status)
{
    internal static GetCourtResponse From(CourtDto court) => new(
        court.Id,
        court.Name,
        court.Surface,
        court.Status);
}
