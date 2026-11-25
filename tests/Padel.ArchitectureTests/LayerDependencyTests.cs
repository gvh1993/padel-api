using NetArchTest.Rules;

namespace Padel.ArchitectureTests;

/// <summary>
/// Dependencies point inwards: Api → Infrastructure → Application → Domain.
/// </summary>
public class LayerDependencyTests
{
    [Test]
    public void Domain_ShouldNotDependOnOtherLayers() =>
        Types.InAssembly(Layers.Domain)
            .ShouldNot()
            .HaveDependencyOnAny(Layers.ApplicationNamespace, Layers.InfrastructureNamespace, Layers.ApiNamespace)
            .ShouldPass();

    [Test]
    public void Domain_ShouldNotDependOnFrameworks() =>
        Types.InAssembly(Layers.Domain)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql", "FluentValidation")
            .ShouldPass();

    [Test]
    public void Application_ShouldNotDependOnInfrastructureOrApi() =>
        Types.InAssembly(Layers.Application)
            .ShouldNot()
            .HaveDependencyOnAny(Layers.InfrastructureNamespace, Layers.ApiNamespace)
            .ShouldPass();

    [Test]
    public void Application_ShouldNotDependOnPersistenceOrWebFrameworks() =>
        Types.InAssembly(Layers.Application)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Npgsql", "Microsoft.AspNetCore", "OpenTelemetry")
            .ShouldPass();

    [Test]
    public void Infrastructure_ShouldNotDependOnApi() =>
        Types.InAssembly(Layers.Infrastructure)
            .ShouldNot()
            .HaveDependencyOn(Layers.ApiNamespace)
            .ShouldPass();

    [Test]
    public void ApiFeatures_ShouldNotUseInfrastructure() =>
        Types.InAssembly(Layers.Api)
            .That().ResideInNamespaceStartingWith($"{Layers.ApiNamespace}.Features")
            .ShouldNot()
            .HaveDependencyOnAny(Layers.InfrastructureNamespace, "Microsoft.EntityFrameworkCore")
            .ShouldPass();
}
