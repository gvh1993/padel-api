using Refit;

namespace Padel.IntegrationTests.Infrastructure;

/// <summary>
/// RFC 9457 problem details as returned by the API.
/// </summary>
public sealed record Problem
{
    public int Status { get; init; }
    public string? Title { get; init; }
    public string? Detail { get; init; }
    public string? TraceId { get; init; }

    /// <summary>The stable code of the application error, e.g. <c>Member.EmailInUse</c>.</summary>
    public string? ErrorCode { get; init; }
    public Dictionary<string, string[]> Errors { get; init; } = [];
}

public static class ApiResponseExtensions
{
    public static async Task<Problem> ReadProblemAsync(this IApiResponse response)
    {
        var error = response.Error as ApiException
            ?? throw new InvalidOperationException($"Expected an error response but got {(int?)response.StatusCode}.");

        return await error.GetContentAsAsync<Problem>()
            ?? throw new InvalidOperationException("The response did not contain problem details.");
    }

    public static T Value<T>(this IApiResponse<T> response) =>
        response.IsSuccessStatusCode && response.Content is not null
            ? response.Content
            : throw new InvalidOperationException(
                $"Expected success but got {(int?)response.StatusCode}: {(response.Error as ApiException)?.Content}");
}
