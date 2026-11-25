using Padel.Domain.Courts;

namespace Padel.Application.Features.Courts;

public sealed record CourtDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Surface { get; init; } = string.Empty;
    public CourtStatus Status { get; init; }
}
