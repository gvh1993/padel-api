using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Padel.Application.Common;

namespace Padel.Api.Authentication;

internal static class AuthenticationSetup
{
    /// <summary>
    /// JWT bearer authentication against Keycloak (section <c>Keycloak</c>). Every endpoint requires an
    /// authenticated user unless it opts out; admin endpoints use <see cref="PadelAuthorization.AdminPolicy"/>.
    /// </summary>
    public static IServiceCollection AddPadelAuthentication(this IServiceCollection services)
    {
        services.AddOptions<KeycloakOptions>()
            .BindConfiguration(KeycloakOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<KeycloakOptions>>((jwt, keycloakOptions) =>
            {
                var keycloak = keycloakOptions.Value;

                // Issuer and signing keys come from {Authority}/.well-known/openid-configuration.
                jwt.Authority = keycloak.Authority;
                jwt.Audience = keycloak.Audience;
                jwt.RequireHttpsMetadata = keycloak.RequireHttpsMetadata;

                // Keep the claim names from the token (no mapping to the long WS-* claim types).
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters.NameClaimType = PadelAuthorization.NameClaim;
                jwt.TokenValidationParameters.RoleClaimType = PadelAuthorization.RolesClaim;
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(PadelAuthorization.AdminPolicy, policy => policy.RequireRole(PadelAuthorization.AdminRole));

        services.AddMemoryCache();
        services.AddScoped<IClaimsTransformation, MemberClaimsTransformation>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        services.AddHttpClient(KeycloakHealthCheck.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(5));
        services.AddHealthChecks().AddCheck<KeycloakHealthCheck>("keycloak");

        return services;
    }
}
