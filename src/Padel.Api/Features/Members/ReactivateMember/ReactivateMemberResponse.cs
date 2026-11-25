using Padel.Application.Features.Members;
using Padel.Domain.Members;

namespace Padel.Api.Features.Members;

/// <summary>The reactivated member.</summary>
public sealed record ReactivateMemberResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    MemberStatus Status,
    string? SuspensionReason,
    DateTimeOffset RegisteredOn)
{
    internal static ReactivateMemberResponse From(MemberDto member) => new(
        member.Id,
        member.FirstName,
        member.LastName,
        member.Email,
        member.Phone,
        member.Status,
        member.SuspensionReason,
        member.RegisteredOn);
}
