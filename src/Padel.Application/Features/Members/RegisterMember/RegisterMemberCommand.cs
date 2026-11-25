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
    public async Task<MemberDto> Handle(RegisterMemberCommand request, CancellationToken cancellationToken)
    {
        var email = new Email(request.Email);

        if (await members.GetByEmailAsync(email, cancellationToken) is not null)
        {
            throw new ConflictException($"A member with email '{email}' already exists.");
        }

        var member = new Member(
            new FullName(request.FirstName, request.LastName),
            email,
            request.Phone,
            timeProvider.GetUtcNow());

        members.Add(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return member.ToDto();
    }
}
