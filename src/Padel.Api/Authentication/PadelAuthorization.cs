namespace Padel.Api.Authentication;

public static class PadelAuthorization
{
    /// <summary>Policy for club administration endpoints.</summary>
    public const string AdminPolicy = "Admin";

    /// <summary>Keycloak realm role that grants admin access.</summary>
    public const string AdminRole = "admin";

    /// <summary>Claim with the realm roles; the realm's <c>roles</c> mapper puts them at the top level of the token.</summary>
    public const string RolesClaim = "roles";

    /// <summary>Claim that links the caller to a member; mapped from the Keycloak user attribute <c>member_id</c>.</summary>
    public const string MemberIdClaim = "member_id";

    /// <summary>Claim used as the user name.</summary>
    public const string NameClaim = "preferred_username";
}
