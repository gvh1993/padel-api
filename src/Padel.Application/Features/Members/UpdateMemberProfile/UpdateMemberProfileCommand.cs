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

    /// <summary>The version the client changes (HTTP <c>If-Match</c>); <c>null</c> skips the check.</summary>
    public uint? ExpectedVersion { get; init; }
}

public sealed class UpdateMemberProfileCommandHandler(
    IMemberRepository members,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IRequestHandler<UpdateMemberProfileCommand, MemberDto>
{
    public async Task<Result<MemberDto>> Handle(UpdateMemberProfileCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.CanAccessMember(request.Id) is Error accessDenied)
        {
            return accessDenied;
        }

        var member = await members.GetByIdAsync(request.Id, cancellationToken);
        if (member is null)
        {
            return ApplicationErrors.NotFound<Member>(request.Id);
        }

        if (member.MatchesVersion(request.ExpectedVersion) is Error stale)
        {
            return stale;
        }

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

        var owner = await members.GetByEmailAsync(email, cancellationToken);
        if (owner is not null && owner.Id != member.Id)
        {
            return MemberErrors.EmailInUse(email);
        }

        if (member.UpdateProfile(name, email, request.Phone) is Error updatedError)
        {
            return updatedError;
        }

        return await unitOfWork.SaveChangesAsync(cancellationToken) is Error saveError ? saveError : member.ToDto();
    }
}
