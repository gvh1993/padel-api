using AwesomeAssertions;
using NetArchTest.Rules;
using Padel.Application.Common.Messaging;

namespace Padel.ArchitectureTests;

/// <summary>
/// Features are vertical slices. Bookings may use members and courts (a booking needs both),
/// but members and courts know nothing about other features.
/// </summary>
public class VerticalSliceTests
{
    private const string Features = $"{Layers.ApplicationNamespace}.Features";

    [Test]
    [Arguments("Members", new[] { "Courts", "Bookings" })]
    [Arguments("Courts", new[] { "Members", "Bookings" })]
    public void Feature_ShouldNotDependOnOtherFeatures(string feature, string[] otherFeatures) =>
        Types.InAssembly(Layers.Application)
            .That().ResideInNamespace($"{Features}.{feature}")
            .ShouldNot()
            .HaveDependencyOnAny(otherFeatures.Select(other => $"{Features}.{other}").ToArray())
            .ShouldPass();

    [Test]
    public void Requests_ShouldLiveInAFeature() =>
        Types.InAssembly(Layers.Application)
            .That().ImplementInterface(typeof(IRequest<>))
            .Should().ResideInNamespaceStartingWith(Features)
            .ShouldPass();

    [Test]
    public void EveryRequest_ShouldHaveExactlyOneHandler()
    {
        var types = Layers.Application.GetTypes();

        var handledRequests = types
            .SelectMany(type => type.GetInterfaces())
            .Where(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
            .Select(contract => contract.GetGenericArguments()[0])
            .ToList();

        var withoutExactlyOneHandler = types
            .Where(type => !type.IsAbstract && type.GetInterfaces()
                .Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequest<>)))
            .Where(request => handledRequests.Count(handled => handled == request) != 1)
            .Select(request => request.Name);

        withoutExactlyOneHandler.Should().BeEmpty();
    }

    /// <summary>
    /// REPR: every use case has exactly one endpoint with the same name (<c>CancelBookingCommand</c> ↔
    /// <c>CancelBookingEndpoint</c>), in the same feature, and the other way round.
    /// </summary>
    [Test]
    public void EveryRequest_ShouldHaveOneEndpointInTheSameFeature()
    {
        var useCases = Layers.Application.GetTypes()
            .Where(type => !type.IsAbstract && type.GetInterfaces()
                .Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequest<>)))
            .Select(request => $"{FeatureOf(request.Namespace!)}.{TrimSuffix(request.Name, "Command", "Query")}")
            .Order()
            .ToList();

        var endpoints = Layers.ApiEndpoints()
            .Select(endpoint => $"{FeatureOf(endpoint.Namespace!)}.{TrimSuffix(endpoint.Name, "Endpoint")}")
            .Order()
            .ToList();

        endpoints.Should().Equal(useCases);
    }

    /// <summary>
    /// REPR: requests and responses are the API's own contracts. Their public members may use domain
    /// enums and framework types, but never application DTOs, commands or queries.
    /// </summary>
    [Test]
    public void ApiContracts_ShouldNotExposeApplicationTypes()
    {
        var contracts = Layers.Api.GetExportedTypes()
            .Where(type => type.Namespace?.StartsWith($"{Layers.ApiNamespace}.Features", StringComparison.Ordinal) == true);

        var leaks = contracts
            .SelectMany(type => type.GetProperties().Select(property => (type, property)))
            .Where(member => ExposedTypes(member.property.PropertyType).Any(exposed => exposed.Assembly == Layers.Application))
            .Select(member => $"{member.type.Name}.{member.property.Name}");

        leaks.Should().BeEmpty();
    }

    private static IEnumerable<Type> ExposedTypes(Type type) =>
        type.IsGenericType
            ? type.GetGenericArguments().SelectMany(ExposedTypes).Append(type)
            : [type];

    private static string FeatureOf(string @namespace) => @namespace[(@namespace.LastIndexOf('.') + 1)..];

    private static string TrimSuffix(string name, params string[] suffixes) =>
        suffixes.FirstOrDefault(suffix => name.EndsWith(suffix, StringComparison.Ordinal)) is { } suffix
            ? name[..^suffix.Length]
            : name;
}
