using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Padel.Api.Authentication;

namespace Padel.Api.OpenApi;

/// <summary>
/// Adds API information and the Keycloak OAuth2 security scheme, so Scalar can sign in.
/// </summary>
internal sealed class PadelDocumentTransformer(IOptions<KeycloakOptions> keycloakOptions) : IOpenApiDocumentTransformer
{
    public const string SecuritySchemeName = "Keycloak";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info.Title = "Padel API";
        document.Info.Description =
            "Members, courts and bookings for a padel club. " +
            "Errors are RFC 9457 problem details. " +
            "List endpoints support `page`, `pageSize` and `sort` (e.g. `lastName,-registeredOn`) plus resource-specific filters.";

        var openIdConnect = keycloakOptions.Value.Authority.TrimEnd('/') + "/protocol/openid-connect";

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SecuritySchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description =
                "Sign in with Keycloak. Admins have the realm role `admin`; " +
                "members have a `member_id` attribute that links them to their member.",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{openIdConnect}/auth"),
                    TokenUrl = new Uri($"{openIdConnect}/token"),
                    Scopes = new Dictionary<string, string> { ["openid"] = "OpenID Connect" },
                },
            },
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SecuritySchemeName, document)] = ["openid"],
        });

        return Task.CompletedTask;
    }
}
