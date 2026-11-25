namespace Padel.Api.Features.Courts;

public sealed record RenameCourtRequest
{
    public required string Name { get; init; }
}

public sealed record SetCourtMaintenanceRequest
{
    public required bool UnderMaintenance { get; init; }
}
