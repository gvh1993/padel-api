namespace Padel.Api.Features.Members;

/// <summary>Body of <c>PUT /api/members/{id}</c>.</summary>
public sealed record UpdateMemberProfileRequest
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
}
