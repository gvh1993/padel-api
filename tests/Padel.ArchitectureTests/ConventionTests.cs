using System.Reflection;
using FluentValidation;
using NetArchTest.Rules;
using Padel.Application.Common.Messaging;
using Padel.Domain.Common;

namespace Padel.ArchitectureTests;

public class ConventionTests
{
    /// <summary>Expected failures are <c>Result</c>s with an <c>Error</c>, never custom exceptions.</summary>
    [Test]
    public Task Layers_Should_NotDefineExceptions() =>
        Types.InAssemblies([Layers.Domain, Layers.Application, Layers.Infrastructure, Layers.Api])
            .ShouldNot()
            .Inherit(typeof(Exception))
            .ShouldPass();

    [Test]
    public Task RequestHandlers_Should_BeSealedAndNamedHandler() =>
        Types.InAssembly(Layers.Application)
            .That().ImplementInterface(typeof(IRequestHandler<,>))
            .Should().BeSealed()
            .And().HaveNameEndingWith("Handler")
            .ShouldPass();

    [Test]
    public Task Validators_Should_BeSealedAndNamedValidator() =>
        Types.InAssembly(Layers.Application)
            .That().Inherit(typeof(AbstractValidator<>))
            .And().AreNotAbstract()
            .Should().BeSealed()
            .And().HaveNameEndingWith("Validator")
            .ShouldPass();

    [Test]
    public Task Requests_Should_BeNamedCommandOrQuery() =>
        Types.InAssembly(Layers.Application)
            .That().ImplementInterface(typeof(IRequest<>))
            .Should().HaveNameEndingWith("Command")
            .Or().HaveNameEndingWith("Query")
            .ShouldPass();

    [Test]
    public Task DomainEvents_Should_BeSealed() =>
        Types.InAssembly(Layers.Domain)
            .That().ImplementInterface(typeof(IDomainEvent))
            .And().AreNotAbstract()
            .Should().BeSealed()
            .ShouldPass();

    [Test]
    public async Task Entities_Should_NotHavePublicSetters()
    {
        var publicSetters = Layers.Domain.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(Entity)))
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.SetMethod?.IsPublic == true)
                .Select(property => $"{type.Name}.{property.Name}"));

        await Assert.That(publicSetters).IsEmpty();
    }

    [Test]
    public async Task Endpoints_Should_BeInternalSealedAndNamedEndpoint()
    {
        var violations = Layers.ApiEndpoints()
            .Where(endpoint => endpoint.IsPublic || !endpoint.IsSealed || !endpoint.Name.EndsWith("Endpoint", StringComparison.Ordinal))
            .Select(endpoint => endpoint.FullName);

        await Assert.That(violations).IsEmpty();
    }
}
