using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Members;

namespace Padel.Application.Features.Members;

public sealed record ReactivateMemberCommand(Guid Id) : IRequest<MemberDto>;

public sealed class ReactivateMemberCommandHandler(
    IMemberRepository members,
    IUnitOfWork unitOfWork) : IRequestHandler<ReactivateMemberCommand, MemberDto>
{
    public async Task<MemberDto> Handle(ReactivateMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await members.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Member>(request.Id);

        member.Reactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return member.ToDto();
    }
}
