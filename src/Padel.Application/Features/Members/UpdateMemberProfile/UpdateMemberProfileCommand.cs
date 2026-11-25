using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Common;
using Padel.Domain.Members;

namespace Padel.Application.Features.Members;

public sealed record UpdateMemberProfileCommand : IRequest<MemberDto>
{
    public required Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
}

public sealed class UpdateMemberProfileCommandHandler(
    IMemberRepository members,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IRequestHandler<UpdateMemberProfileCommand, MemberDto>
{
    public async Task<MemberDto> Handle(UpdateMemberProfileCommand request, CancellationToken cancellationToken)
    {
        currentUser.EnsureCanAccessMember(request.Id);

        var member = await members.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Member>(request.Id);

        var email = new Email(request.Email);
        var owner = await members.GetByEmailAsync(email, cancellationToken);
        if (owner is not null && owner.Id != member.Id)
        {
            throw new ConflictException($"A member with email '{email}' already exists.");
        }

        member.UpdateProfile(new FullName(request.FirstName, request.LastName), email, request.Phone);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return member.ToDto();
    }
}
