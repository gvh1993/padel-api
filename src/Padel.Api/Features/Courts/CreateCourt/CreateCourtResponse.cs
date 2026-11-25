using Padel.Application.Features.Courts;
using Padel.Domain.Courts;

namespace Padel.Api.Features.Courts;

/// <summary>The created court.</summary>
public sealed record CreateCourtResponse(
    Guid Id,
    string Name,
    string Surface,
    CourtStatus Status)
{
    internal static CreateCourtResponse From(CourtDto court) => new(
        court.Id,
        court.Name,
        court.Surface,
        court.Status);
}
