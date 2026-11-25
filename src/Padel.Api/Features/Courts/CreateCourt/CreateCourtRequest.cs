namespace Padel.Api.Features.Courts;

/// <summary>Body of <c>POST /api/courts</c>.</summary>
public sealed record CreateCourtRequest
{
    public required string Name { get; init; }

    /// <summary>For example <c>glass</c> or <c>concrete</c>.</summary>
    public required string Surface { get; init; }
}
