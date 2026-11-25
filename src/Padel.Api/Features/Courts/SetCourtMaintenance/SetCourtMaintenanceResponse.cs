using Padel.Application.Features.Courts;
using Padel.Domain.Courts;

namespace Padel.Api.Features.Courts;

/// <summary>The court with its new status.</summary>
public sealed record SetCourtMaintenanceResponse(
    Guid Id,
    string Name,
    string Surface,
    CourtStatus Status)
{
    internal static SetCourtMaintenanceResponse From(CourtDto court) => new(
        court.Id,
        court.Name,
        court.Surface,
        court.Status);
}
