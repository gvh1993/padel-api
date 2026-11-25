using Padel.Application.Common;

namespace Padel.Api.Authentication;

internal sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string? UserId => httpContextAccessor.HttpContext?.User.FindFirst(PadelAuthorization.SubjectClaim)?.Value;

    public Guid? MemberId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirst(PadelAuthorization.MemberIdClaim)?.Value, out var memberId)
            ? memberId
            : null;

    public bool IsAdmin => httpContextAccessor.HttpContext?.User.IsInRole(PadelAuthorization.AdminRole) == true;
}
