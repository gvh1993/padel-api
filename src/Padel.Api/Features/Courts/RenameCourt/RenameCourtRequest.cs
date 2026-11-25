namespace Padel.Api.Features.Courts;

/// <summary>Body of <c>PUT /api/v1/courts/{id}/name</c>.</summary>
public sealed record RenameCourtRequest
{
    public required string Name { get; init; }
}
