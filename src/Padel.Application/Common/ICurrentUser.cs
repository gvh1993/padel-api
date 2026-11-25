namespace Padel.Application.Common;

/// <summary>
/// The authenticated caller of the current request.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The caller's account at the identity provider (the token's <c>sub</c>); <c>null</c> when not signed in.</summary>
    string? UserId { get; }

    /// <summary>The member linked to the caller, if any (the <c>member_id</c> claim).</summary>
    Guid? MemberId { get; }

    bool IsAdmin { get; }
}
