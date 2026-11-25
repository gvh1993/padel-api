using System.ComponentModel.DataAnnotations;

namespace Padel.Api.Authentication;

/// <summary>
/// Keycloak settings (section <c>Keycloak</c>). The Aspire AppHost sets <see cref="Authority"/>.
/// </summary>
public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    /// <summary>The realm URL, e.g. <c>https://localhost:18080/realms/padel</c>. Used as token issuer and by the browser (Scalar).</summary>
    [Required]
    [Url]
    public string Authority { get; set; } = string.Empty;

    /// <summary>The audience every access token must contain.</summary>
    [Required]
    public string Audience { get; set; } = "padel-api";

    /// <summary>Public client that Scalar uses to sign in (authorization code with PKCE).</summary>
    [Required]
    public string DocumentationClientId { get; set; } = "padel-scalar";

    /// <summary>Only disable for local development and tests, where Keycloak runs on plain HTTP.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}
