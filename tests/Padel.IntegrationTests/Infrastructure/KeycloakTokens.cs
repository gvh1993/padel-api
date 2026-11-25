using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Padel.Api.Authentication;

namespace Padel.IntegrationTests.Infrastructure;

/// <summary>
/// Gets real access tokens from the Keycloak test container, using the realm the AppHost imports
/// (src/Padel.AppHost/Realms). A member's account has the member's email address; the API links them
/// on the first sign-in when Keycloak marks the address as verified.
/// </summary>
public sealed class KeycloakTokens(HttpClient keycloak, string adminUsername, string adminPassword)
{
    public const string Realm = "padel";
    private const string ClientId = "padel-scalar";

    // Users from the realm file.
    private const string ClubAdmin = "admin";
    private const string UnlinkedMember = "member";

    private readonly ConcurrentDictionary<string, Lazy<Task<string>>> _tokens = new();

    public string Authority => new Uri(keycloak.BaseAddress!, $"realms/{Realm}").ToString();

    public Task<string> AdminAsync() => Cached(ClubAdmin, () => PasswordGrantAsync(ClubAdmin, ClubAdmin));

    /// <summary>A user without <c>member_id</c>: authenticated, but not linked to a member.</summary>
    public Task<string> WithoutMemberLinkAsync() =>
        Cached(UnlinkedMember, () => PasswordGrantAsync(UnlinkedMember, UnlinkedMember));

    /// <summary>An account with the member's email address, verified unless said otherwise.</summary>
    public Task<string> ForEmailAsync(string email, bool emailVerified = true) => Cached($"{email}:{emailVerified}", async () =>
    {
        var username = $"user-{Guid.NewGuid():N}";
        var password = Guid.NewGuid().ToString("N");

        await CreateUserAsync(new
        {
            username,
            enabled = true,
            email,
            emailVerified,
            firstName = "Test",
            lastName = "Member",
            credentials = new[] { new { type = "password", value = password, temporary = false } },
        });

        return await PasswordGrantAsync(username, password);
    });

    /// <summary>A token that looks like an admin token for this realm, but is signed with a key Keycloak never issued.</summary>
    public string Forged()
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa.ExportParameters(includePrivateParameters: true)) { KeyId = "forged" };

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Authority,
            Audience = "padel-api",
            Claims = new Dictionary<string, object>
            {
                ["sub"] = Guid.NewGuid().ToString(),
                [PadelAuthorization.RolesClaim] = new[] { PadelAuthorization.AdminRole },
            },
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256),
        });
    }

    private Task<string> Cached(string key, Func<Task<string>> create) =>
        _tokens.GetOrAdd(key, _ => new Lazy<Task<string>>(create)).Value;

    private async Task<string> PasswordGrantAsync(string username, string password, string realm = Realm, string clientId = ClientId)
    {
        using var response = await keycloak.PostAsync(
            $"realms/{realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = clientId,
                ["username"] = username,
                ["password"] = password,
                ["scope"] = "openid",
            }));

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Keycloak refused a token for '{username}' ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync()}");
        }

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return token!.AccessToken;
    }

    private async Task CreateUserAsync(object user)
    {
        var adminToken = await PasswordGrantAsync(adminUsername, adminPassword, realm: "master", clientId: "admin-cli");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"admin/realms/{Realm}/users") { Content = JsonContent.Create(user) };
        request.Headers.Authorization = new("Bearer", adminToken);

        using var response = await keycloak.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Keycloak refused to create a user ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync()}");
        }
    }

    private sealed record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
}
