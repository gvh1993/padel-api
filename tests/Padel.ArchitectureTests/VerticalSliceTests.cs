using NetArchTest.Rules;
using Padel.Application.Common.Messaging;
using TUnit.Assertions.Enums;

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
    public Task Feature_Should_NotDependOnOtherFeatures(string feature, string[] otherFeatures) =>
        Types.InAssembly(Layers.Application)
            .That().ResideInNamespace($"{Features}.{feature}")
            .ShouldNot()
            .HaveDependencyOnAny(otherFeatures.Select(other => $"{Features}.{other}").ToArray())
            .ShouldPass();

    [Test]
    public Task Requests_Should_LiveInAFeature() =>
        Types.InAssembly(Layers.Application)
            .That().ImplementInterface(typeof(IRequest<>))
            .Should().ResideInNamespaceStartingWith(Features)
            .ShouldPass();

    [Test]
    public async Task EveryRequest_Should_HaveExactlyOneHandler()
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

        await Assert.That(withoutExactlyOneHandler).IsEmpty();
    }

    /// <summary>
    /// REPR: every use case has exactly one endpoint with the same name (<c>CancelBookingCommand</c> ↔
    /// <c>CancelBookingEndpoint</c>), in the same feature, and the other way round.
    /// </summary>
    [Test]
    public async Task EveryRequest_Should_HaveOneEndpointInTheSameFeature()
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

        await Assert.That(endpoints).IsEquivalentTo(useCases, CollectionOrdering.Matching);
    }

    /// <summary>
    /// REPR: requests and responses are the API's own contracts. Their public members may use domain
    /// enums and framework types, but never application DTOs, commands or queries.
    /// </summary>
    [Test]
    public async Task ApiContracts_Should_NotExposeApplicationTypes()
    {
        var contracts = Layers.Api.GetExportedTypes()
            .Where(type => type.Namespace?.StartsWith($"{Layers.ApiNamespace}.Features", StringComparison.Ordinal) == true);

        var leaks = contracts
            .SelectMany(type => type.GetProperties().Select(property => (type, property)))
            .Where(member => ExposedTypes(member.property.PropertyType).Any(exposed => exposed.Assembly == Layers.Application))
            .Select(member => $"{member.type.Name}.{member.property.Name}");

        await Assert.That(leaks).IsEmpty();
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
