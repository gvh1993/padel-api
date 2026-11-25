namespace Padel.Application.Common;

/// <summary>
/// The authenticated caller of the current request.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The member linked to the caller, if any (the <c>member_id</c> claim).</summary>
    Guid? MemberId { get; }

    bool IsAdmin { get; }
}

public static class CurrentUserExtensions
{
    /// <summary>
    /// Admins may access every member; other users only their own member data.
    /// </summary>
    public static void EnsureCanAccessMember(this ICurrentUser currentUser, Guid memberId)
    {
        if (currentUser.IsAdmin || currentUser.MemberId == memberId)
        {
            return;
        }

        throw new ForbiddenException();
    }
}
