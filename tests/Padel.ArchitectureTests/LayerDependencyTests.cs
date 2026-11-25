using NetArchTest.Rules;

namespace Padel.ArchitectureTests;

/// <summary>
/// Dependencies point inwards: Api → Infrastructure → Application → Domain.
/// </summary>
public class LayerDependencyTests
{
    [Test]
    public Task Domain_Should_NotDependOnOtherLayers() =>
        Types.InAssembly(Layers.Domain)
            .ShouldNot()
            .HaveDependencyOnAny(Layers.ApplicationNamespace, Layers.InfrastructureNamespace, Layers.ApiNamespace)
            .ShouldPass();

    [Test]
    public Task Domain_Should_NotDependOnFrameworks() =>
        Types.InAssembly(Layers.Domain)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql", "FluentValidation")
            .ShouldPass();

    [Test]
    public Task Application_Should_NotDependOnInfrastructureOrApi() =>
        Types.InAssembly(Layers.Application)
            .ShouldNot()
            .HaveDependencyOnAny(Layers.InfrastructureNamespace, Layers.ApiNamespace)
            .ShouldPass();

    [Test]
    public Task Application_Should_NotDependOnPersistenceOrWebFrameworks() =>
        Types.InAssembly(Layers.Application)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Npgsql", "Microsoft.AspNetCore", "OpenTelemetry")
            .ShouldPass();

    [Test]
    public Task Infrastructure_Should_NotDependOnApi() =>
        Types.InAssembly(Layers.Infrastructure)
            .ShouldNot()
            .HaveDependencyOn(Layers.ApiNamespace)
            .ShouldPass();

    [Test]
    public Task ApiFeatures_Should_NotUseInfrastructure() =>
        Types.InAssembly(Layers.Api)
            .That().ResideInNamespaceStartingWith($"{Layers.ApiNamespace}.Features")
            .ShouldNot()
            .HaveDependencyOnAny(Layers.InfrastructureNamespace, "Microsoft.EntityFrameworkCore")
            .ShouldPass();
}
