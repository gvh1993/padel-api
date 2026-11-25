namespace Padel.Api.Features.Members;

/// <summary>Optional body of <c>POST /api/v1/members/{id}/suspend</c>.</summary>
public sealed record SuspendMemberRequest
{
    public string? Reason { get; init; }
}
