using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Padel.Application.Features.Members;

namespace Padel.Api.Authentication;

/// <summary>
/// Adds the <c>member_id</c> claim for a signed-in account that belongs to a member (see
/// <see cref="MemberIdentityResolver"/>). The API decides who is which member; a <c>member_id</c>
/// claim in the token itself is ignored.
/// </summary>
internal sealed class MemberClaimsTransformation(MemberIdentityResolver resolver, IMemoryCache cache) : IClaimsTransformation
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identity
            || identity.HasClaim(claim => claim.Type == PadelAuthorization.MemberIdClaim && claim.Issuer == ClaimsIdentity.DefaultIssuer))
        {
            return principal;
        }

        foreach (var claim in identity.FindAll(PadelAuthorization.MemberIdClaim).ToList())
        {
            identity.RemoveClaim(claim);
        }

        if (principal.FindFirst(PadelAuthorization.SubjectClaim)?.Value is not { } subject)
        {
            return principal;
        }

        var memberId = await ResolveAsync(subject, VerifiedEmail(principal));
        if (memberId is not null)
        {
            identity.AddClaim(new Claim(PadelAuthorization.MemberIdClaim, memberId.Value.ToString()));
        }

        return principal;
    }

    private async Task<Guid?> ResolveAsync(string subject, string? verifiedEmail)
    {
        var cacheKey = $"member-of:{subject}";
        if (cache.TryGetValue(cacheKey, out Guid cached))
        {
            return cached;
        }

        var memberId = await resolver.ResolveAsync(subject, verifiedEmail);
        if (memberId is not null)
        {
            // Links never change, so a found member can be cached; a missing one is looked up again.
            cache.Set(cacheKey, memberId.Value, CacheDuration);
        }

        return memberId;
    }

    private static string? VerifiedEmail(ClaimsPrincipal principal) =>
        principal.FindFirst(PadelAuthorization.EmailVerifiedClaim)?.Value == "true"
            ? principal.FindFirst(PadelAuthorization.EmailClaim)?.Value
            : null;
}
