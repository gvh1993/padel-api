using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Common;
using Padel.Domain.Members;

namespace Padel.Application.Features.Members;

public sealed record SuspendMemberCommand(Guid Id, string? Reason, uint? ExpectedVersion = null) : IRequest<MemberDto>;

public sealed class SuspendMemberCommandHandler(
    IMemberRepository members,
    IUnitOfWork unitOfWork) : IRequestHandler<SuspendMemberCommand, MemberDto>
{
    public async Task<Result<MemberDto>> Handle(SuspendMemberCommand request, CancellationToken cancellationToken)
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

        if (member.Suspend(request.Reason) is Error suspendedError)
        {
            return suspendedError;
        }

        return await unitOfWork.SaveChangesAsync(cancellationToken) is Error saveError ? saveError : member.ToDto();
    }
}
