namespace Padel.Api.Features.Members;

public sealed record UpdateMemberProfileRequest
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
}

public sealed record SuspendMemberRequest
{
    public string? Reason { get; init; }
}
