namespace Padel.Api.Http;

/// <summary>
/// One HTTP endpoint (REPR: request, endpoint, response). Each lives in its own use-case folder
/// under <c>Features</c> and is mapped at start-up by <see cref="EndpointRegistration.MapEndpoints"/>.
/// </summary>
internal interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder routes);
}

internal static class EndpointRegistration
{
    /// <summary>Maps every <see cref="IEndpoint"/> in this assembly.</summary>
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var endpoints = typeof(IEndpoint).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(IEndpoint)))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .Select(type => (IEndpoint)Activator.CreateInstance(type)!);

        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(routes);
        }

        return routes;
    }
}
