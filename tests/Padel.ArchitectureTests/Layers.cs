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
}
