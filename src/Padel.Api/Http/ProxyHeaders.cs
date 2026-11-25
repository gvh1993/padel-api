using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Padel.Api.Http;

/// <summary>
/// Behind a reverse proxy or ingress, the client's address and scheme come from <c>X-Forwarded-For</c> and
/// <c>X-Forwarded-Proto</c>. Only proxies in <c>ForwardedHeaders:KnownProxies</c> (IP addresses) or
/// <c>ForwardedHeaders:KnownNetworks</c> (CIDR, e.g. <c>10.0.0.0/8</c>) are trusted, besides localhost;
/// headers from anyone else are ignored, so clients cannot spoof their address.
/// </summary>
internal static class ProxyHeaders
{
    public const string SectionName = "ForwardedHeaders";

    public static IServiceCollection AddPadelForwardedHeaders(this IServiceCollection services)
    {
        services.AddOptions<ForwardedHeadersOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                | ForwardedHeaders.XForwardedProto;

            var section = configuration.GetSection(SectionName);
            foreach (var proxy in section.GetSection("KnownProxies").Get<string[]>() ?? [])
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }

            foreach (var network in section.GetSection("KnownNetworks").Get<string[]>() ?? [])
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });

        return services;
    }
}
