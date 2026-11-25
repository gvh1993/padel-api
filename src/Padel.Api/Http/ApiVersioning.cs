using System.Globalization;
using Asp.Versioning;

namespace Padel.Api.Http;

/// <summary>
/// API versions in the URL: <c>/api/v1/members</c>. Every response reports the supported versions in the
/// <c>api-supported-versions</c> header (and deprecated ones in <c>api-deprecated-versions</c>).
/// Each version has its own OpenAPI document: <c>/openapi/v1.json</c>.
/// </summary>
internal static class ApiVersioning
{
    public static readonly ApiVersion V1 = new(1);

    /// <summary>Every version that has endpoints; each gets an OpenAPI document.</summary>
    public static readonly IReadOnlyList<ApiVersion> Versions = [V1];

    /// <summary>"v" plus the version: <c>v1</c>, or <c>v1.1</c> with a minor version.</summary>
    private const string GroupNameFormat = "'v'VVV";

    /// <summary>The OpenAPI document (and API explorer group) of a version: <c>v1</c>.</summary>
    public static string DocumentName(ApiVersion version) => version.ToString(GroupNameFormat, CultureInfo.InvariantCulture);

    public static IServiceCollection AddPadelApiVersioning(this IServiceCollection services)
    {
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = V1;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                // Group per version ("v1") so each OpenAPI document only has its version's endpoints,
                // with the version filled in the paths (/api/v1/members instead of /api/v{version}/members).
                options.GroupNameFormat = GroupNameFormat;
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }

    /// <summary>
    /// The <c>/api/v{version}</c> group that every feature group maps onto. Endpoints belong to all versions
    /// in <see cref="Versions"/>, unless they narrow it with <c>MapToApiVersion</c>.
    /// </summary>
    public static RouteGroupBuilder MapVersionedApi(this IEndpointRouteBuilder routes)
    {
        var api = routes.NewVersionedApi("Padel").MapGroup("/api/v{version:apiVersion}");

        foreach (var version in Versions)
        {
            api.HasApiVersion(version);
        }

        return api;
    }
}
