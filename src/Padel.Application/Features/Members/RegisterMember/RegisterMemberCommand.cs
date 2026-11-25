using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Common;
using Padel.Domain.Members;

namespace Padel.Application.Features.Members;

public sealed record RegisterMemberCommand : IRequest<MemberDto>
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
}

public sealed class RegisterMemberCommandHandler(
    IMemberRepository members,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<RegisterMemberCommand, MemberDto>
{
    public async Task<Result<MemberDto>> Handle(RegisterMemberCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult is not Email email)
        {
            return emailResult.Error;
        }

        var nameResult = FullName.Create(request.FirstName, request.LastName);
        if (nameResult is not FullName name)
        {
            return nameResult.Error;
        }

        if (await members.GetByEmailAsync(email, cancellationToken) is not null)
        {
            return MemberErrors.EmailInUse(email);
        }

        var memberResult = Member.Register(name, email, request.Phone, timeProvider.GetUtcNow());
        if (memberResult is not Member member)
        {
            return memberResult.Error;
        }

        members.Add(member);
        return await unitOfWork.SaveChangesAsync(cancellationToken) is Error saveError ? saveError : member.ToDto();
    }
}
