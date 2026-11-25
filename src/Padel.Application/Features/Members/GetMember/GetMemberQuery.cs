using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Members;

namespace Padel.Application.Features.Members;

public sealed record GetMemberQuery(Guid Id) : IRequest<MemberDto>;

public sealed class GetMemberQueryHandler(
    IMemberRepository members,
    ICurrentUser currentUser) : IRequestHandler<GetMemberQuery, MemberDto>
{
    public async Task<MemberDto> Handle(GetMemberQuery request, CancellationToken cancellationToken)
    {
        currentUser.EnsureCanAccessMember(request.Id);

        var member = await members.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Member>(request.Id);

        return member.ToDto();
    }
}
