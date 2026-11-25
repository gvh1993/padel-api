using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Padel.Api.Authentication;

/// <summary>
/// Checks that Keycloak's OpenID configuration can be fetched.
/// Not part of readiness: tokens are validated with cached signing keys, so a short Keycloak outage
/// should not take the API out of service. It does show up in <c>/health</c> and in the Aspire dashboard.
/// </summary>
internal sealed class KeycloakHealthCheck(IHttpClientFactory httpClientFactory, IOptions<KeycloakOptions> options) : IHealthCheck
{
    public const string HttpClientName = "keycloak-health";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var discoveryUrl = $"{options.Value.Authority.TrimEnd('/')}/.well-known/openid-configuration";

        try
        {
            using var response = await httpClientFactory.CreateClient(HttpClientName).GetAsync(discoveryUrl, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"Keycloak returned {(int)response.StatusCode} for the OpenID configuration.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return HealthCheckResult.Unhealthy("Keycloak is unreachable.", exception);
        }
    }
}
