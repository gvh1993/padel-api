using Padel.Application.Common.Querying;
using Padel.Domain.Members;

namespace Padel.Application.Features.Members;

public static class MemberSorting
{
    public static readonly SortMap<Member> Map = new SortMap<Member>("lastName,firstName")
        .Add("firstName", member => member.Name.First)
        .Add("lastName", member => member.Name.Last)
        .Add("email", member => member.Email.Value)
        .Add("status", member => member.Status)
        .Add("registeredOn", member => member.RegisteredOn);
}
