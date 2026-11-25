namespace Padel.Api.Features.Members;

/// <summary>Body of <c>POST /api/v1/members</c>.</summary>
public sealed record RegisterMemberRequest
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
}
