namespace Microsoft.Extensions.Hosting;

/// <summary>Tags that decide which health endpoint runs a check.</summary>
public static class HealthCheckTags
{
    /// <summary>The process is running and responsive. A failing liveness check means: restart me.</summary>
    public const string Live = "live";

    /// <summary>The service can handle requests (database, identity provider). Failing means: send no traffic.</summary>
    public const string Ready = "ready";
}
