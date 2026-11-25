using Padel.Domain.Members;

namespace Padel.Application.Features.Members;

public static class MemberMappings
{
    public static MemberDto ToDto(this Member member) => new()
    {
        Id = member.Id,
        Version = member.Version,
        FirstName = member.Name.First,
        LastName = member.Name.Last,
        Email = member.Email.Value,
        Phone = member.Phone,
        Status = member.Status,
        SuspensionReason = member.SuspensionReason,
        RegisteredOn = member.RegisteredOn,
    };
}
