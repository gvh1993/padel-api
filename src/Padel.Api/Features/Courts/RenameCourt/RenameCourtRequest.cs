namespace Padel.Api.Features.Courts;

/// <summary>Body of <c>PUT /api/courts/{id}/name</c>.</summary>
public sealed record RenameCourtRequest
{
    public required string Name { get; init; }
}
