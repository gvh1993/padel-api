using Refit;

namespace Padel.IntegrationTests.Clients;

/// <summary>
/// Untyped calls for content negotiation and documentation endpoints.
/// </summary>
public interface IRawApi
{
    [Get("/{**path}")]
    Task<HttpResponseMessage> Get(
        string path,
        [Header("Accept")] string accept = "application/json",
        [Query] Dictionary<string, string>? query = null);
}
