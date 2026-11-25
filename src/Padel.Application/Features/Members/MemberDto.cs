using Padel.Domain.Members;

namespace Padel.Application.Features.Members;

public sealed record MemberDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public MemberStatus Status { get; init; }
    public string? SuspensionReason { get; init; }
    public DateTimeOffset RegisteredOn { get; init; }
}
