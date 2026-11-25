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
}
