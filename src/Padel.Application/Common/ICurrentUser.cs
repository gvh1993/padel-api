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
