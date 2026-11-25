using System.Reflection;
using AwesomeAssertions;
using FluentValidation;
using NetArchTest.Rules;
using Padel.Application.Common.Messaging;
using Padel.Domain.Common;

namespace Padel.ArchitectureTests;

public class ConventionTests
{
    /// <summary>Expected failures are <c>Result</c>s with an <c>Error</c>, never custom exceptions.</summary>
    [Test]
    public void Layers_ShouldNotDefineExceptions() =>
        Types.InAssemblies([Layers.Domain, Layers.Application, Layers.Infrastructure, Layers.Api])
            .ShouldNot()
            .Inherit(typeof(Exception))
            .ShouldPass();

    [Test]
    public void RequestHandlers_ShouldBeSealedAndNamedHandler() =>
        Types.InAssembly(Layers.Application)
            .That().ImplementInterface(typeof(IRequestHandler<,>))
            .Should().BeSealed()
            .And().HaveNameEndingWith("Handler")
            .ShouldPass();

    [Test]
    public void Validators_ShouldBeSealedAndNamedValidator() =>
        Types.InAssembly(Layers.Application)
            .That().Inherit(typeof(AbstractValidator<>))
            .And().AreNotAbstract()
            .Should().BeSealed()
            .And().HaveNameEndingWith("Validator")
            .ShouldPass();

    [Test]
    public void Requests_ShouldBeNamedCommandOrQuery() =>
        Types.InAssembly(Layers.Application)
            .That().ImplementInterface(typeof(IRequest<>))
            .Should().HaveNameEndingWith("Command")
            .Or().HaveNameEndingWith("Query")
            .ShouldPass();

    [Test]
    public void DomainEvents_ShouldBeSealed() =>
        Types.InAssembly(Layers.Domain)
            .That().ImplementInterface(typeof(IDomainEvent))
            .And().AreNotAbstract()
            .Should().BeSealed()
            .ShouldPass();

    [Test]
    public void Entities_ShouldNotHavePublicSetters()
    {
        var publicSetters = Layers.Domain.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(Entity)))
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.SetMethod?.IsPublic == true)
                .Select(property => $"{type.Name}.{property.Name}"));

        publicSetters.Should().BeEmpty();
    }

    [Test]
    public void Endpoints_ShouldBeInternalSealedAndNamedEndpoint()
    {
        var violations = Layers.ApiEndpoints()
            .Where(endpoint => endpoint.IsPublic || !endpoint.IsSealed || !endpoint.Name.EndsWith("Endpoint", StringComparison.Ordinal))
            .Select(endpoint => endpoint.FullName);

        violations.Should().BeEmpty();
    }
}
