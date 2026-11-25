using Padel.Api.Http;
using Padel.Application.Features.Members;
using Padel.Domain.Members;

namespace Padel.Api.Features.Members;

/// <summary>One page of members.</summary>
public sealed record ListMembersResponse : PagedResponse<ListMembersItem>;

/// <summary>A member in the list.</summary>
public sealed record ListMembersItem(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    MemberStatus Status,
    string? SuspensionReason,
    DateTimeOffset RegisteredOn)
{
    internal static ListMembersItem From(MemberDto member) => new(
        member.Id,
        member.FirstName,
        member.LastName,
        member.Email,
        member.Phone,
        member.Status,
        member.SuspensionReason,
        member.RegisteredOn);
}
