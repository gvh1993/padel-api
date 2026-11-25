using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Members;

namespace Padel.Application.Features.Members;

public sealed record SuspendMemberCommand(Guid Id, string? Reason) : IRequest<MemberDto>;

public sealed class SuspendMemberCommandHandler(
    IMemberRepository members,
    IUnitOfWork unitOfWork) : IRequestHandler<SuspendMemberCommand, MemberDto>
{
    public async Task<MemberDto> Handle(SuspendMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await members.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Member>(request.Id);

        member.Suspend(request.Reason);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return member.ToDto();
    }
}
