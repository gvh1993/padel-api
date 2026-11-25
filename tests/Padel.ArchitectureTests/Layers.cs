using System.Reflection;
using Padel.Application;
using Padel.Domain.Common;
using Padel.Infrastructure;

namespace Padel.ArchitectureTests;

internal static class Layers
{
    public const string DomainNamespace = "Padel.Domain";
    public const string ApplicationNamespace = "Padel.Application";
    public const string InfrastructureNamespace = "Padel.Infrastructure";
    public const string ApiNamespace = "Padel.Api";

    public static readonly Assembly Domain = typeof(Entity).Assembly;
    public static readonly Assembly Application = typeof(ApplicationServiceCollection).Assembly;
    public static readonly Assembly Infrastructure = typeof(InfrastructureServiceCollection).Assembly;
    public static readonly Assembly Api = typeof(Program).Assembly;

    /// <summary>The REPR endpoint classes (implementations of the internal <c>Padel.Api.Http.IEndpoint</c>).</summary>
    public static IReadOnlyList<Type> ApiEndpoints()
    {
        var endpointInterface = Api.GetType("Padel.Api.Http.IEndpoint", throwOnError: true)!;

        return Api.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(endpointInterface))
            .ToList();
    }
}
