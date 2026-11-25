using Padel.Domain.Common;

namespace Padel.Application.Common;

public static class CurrentUserExtensions
{
    /// <summary>
    /// Admins may access every member; other users only their own member data.
    /// </summary>
    public static Result CanAccessMember(this ICurrentUser currentUser, Guid memberId) =>
        currentUser.IsAdmin || currentUser.MemberId == memberId
            ? Success.Instance
            : ApplicationErrors.Forbidden;
}
