using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Common;
using Padel.Domain.Members;

namespace Padel.Application.Features.Members;

public sealed record ReactivateMemberCommand(Guid Id, uint? ExpectedVersion = null) : IRequest<MemberDto>;

public sealed class ReactivateMemberCommandHandler(
    IMemberRepository members,
    IUnitOfWork unitOfWork) : IRequestHandler<ReactivateMemberCommand, MemberDto>
{
    public async Task<Result<MemberDto>> Handle(ReactivateMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await members.GetByIdAsync(request.Id, cancellationToken);
        if (member is null)
        {
            return ApplicationErrors.NotFound<Member>(request.Id);
        }

        if (member.MatchesVersion(request.ExpectedVersion) is Error stale)
        {
            return stale;
        }

        member.Reactivate();

        return await unitOfWork.SaveChangesAsync(cancellationToken) is Error saveError ? saveError : member.ToDto();
    }
}
