using Padel.Application.Common;
using Padel.Domain.Common;

namespace Padel.Application.Features.Members;

/// <summary>
/// Finds the member that belongs to a signed-in account. A member registered by an admin is linked
/// automatically the first time an account with the same, verified email address signs in; after
/// that the account (not the email) identifies the member.
/// </summary>
public sealed class MemberIdentityResolver(IMemberRepository members, IUnitOfWork unitOfWork)
{
    /// <param name="identityId">The account at the identity provider (the token's <c>sub</c>).</param>
    /// <param name="verifiedEmail">The account's email address, only when the identity provider verified it.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The member's id, or <c>null</c> when the account belongs to no member.</returns>
    public async Task<Guid?> ResolveAsync(string identityId, string? verifiedEmail, CancellationToken cancellationToken = default)
    {
        if (await members.GetByIdentityIdAsync(identityId, cancellationToken) is { } linked)
        {
            return linked.Id;
        }

        if (verifiedEmail is null || Email.Create(verifiedEmail) is not Email email)
        {
            return null;
        }

        // Only a member that is not linked yet can be claimed by email.
        if (await members.GetByEmailAsync(email, cancellationToken) is not { IdentityId: null } member)
        {
            return null;
        }

        if (member.LinkIdentity(identityId) is Error)
        {
            return null;
        }

        // A concurrent first sign-in may have linked it already (unique index): then this one is not linked.
        return await unitOfWork.SaveChangesAsync(cancellationToken) is Error ? null : member.Id;
    }
}
