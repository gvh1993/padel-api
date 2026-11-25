using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Common;
using Padel.Domain.Members;

namespace Padel.Application.Features.Members;

public sealed record GetMemberQuery(Guid Id) : IRequest<MemberDto>;

public sealed class GetMemberQueryHandler(
    IMemberRepository members,
    ICurrentUser currentUser) : IRequestHandler<GetMemberQuery, MemberDto>
{
    public async Task<Result<MemberDto>> Handle(GetMemberQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.CanAccessMember(request.Id) is Error accessDenied)
        {
            return accessDenied;
        }

        var member = await members.GetByIdAsync(request.Id, cancellationToken);
        return member is null ? ApplicationErrors.NotFound<Member>(request.Id) : member.ToDto();
    }
}
