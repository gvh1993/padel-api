using Padel.Application.Features.Courts;
using Padel.Domain.Courts;

namespace Padel.Api.Features.Courts;

/// <summary>The retired court.</summary>
public sealed record RetireCourtResponse(
    Guid Id,
    string Name,
    string Surface,
    CourtStatus Status)
{
    internal static RetireCourtResponse From(CourtDto court) => new(
        court.Id,
        court.Name,
        court.Surface,
        court.Status);
}
