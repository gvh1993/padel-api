using Padel.Application.Features.Members;
using Padel.Domain.Members;

namespace Padel.Api.Features.Members;

/// <summary>The suspended member.</summary>
public sealed record SuspendMemberResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    MemberStatus Status,
    string? SuspensionReason,
    DateTimeOffset RegisteredOn)
{
    internal static SuspendMemberResponse From(MemberDto member) => new(
        member.Id,
        member.FirstName,
        member.LastName,
        member.Email,
        member.Phone,
        member.Status,
        member.SuspensionReason,
        member.RegisteredOn);
}
