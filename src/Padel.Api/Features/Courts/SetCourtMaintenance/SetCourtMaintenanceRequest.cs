namespace Padel.Api.Features.Courts;

/// <summary>Body of <c>PUT /api/v1/courts/{id}/maintenance</c>.</summary>
public sealed record SetCourtMaintenanceRequest
{
    public required bool UnderMaintenance { get; init; }
}
