using Padel.Application.Features.Courts;
using Padel.Domain.Courts;

namespace Padel.Api.Features.Courts;

/// <summary>The renamed court.</summary>
public sealed record RenameCourtResponse(
    Guid Id,
    string Name,
    string Surface,
    CourtStatus Status)
{
    internal static RenameCourtResponse From(CourtDto court) => new(
        court.Id,
        court.Name,
        court.Surface,
        court.Status);
}
