using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Padel.Application.Common.Behaviors;
using Padel.Application.Common.Messaging;

namespace Padel.Application;

public static class ApplicationServiceCollection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceCollection).Assembly;

        services.AddClosedImplementationsOf(typeof(IRequestHandler<,>), assembly);
        services.AddDomainEventHandlersFromAssembly(assembly);

        // Registration order is pipeline order: the first behavior is the outermost one.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TelemetryBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.TryAddScoped<IRequestDispatcher, RequestDispatcher>();
        services.TryAddScoped<IDomainEventPublisher, DomainEventPublisher>();
        services.TryAddSingleton(TimeProvider.System);

        services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Scoped);

        return services;
    }

    /// <summary>
    /// Registers every <see cref="IDomainEventHandler{TEvent}"/> implementation in the assembly.
    /// </summary>
    public static IServiceCollection AddDomainEventHandlersFromAssembly(this IServiceCollection services, Assembly assembly) =>
        services.AddClosedImplementationsOf(typeof(IDomainEventHandler<>), assembly);

    private static IServiceCollection AddClosedImplementationsOf(this IServiceCollection services, Type openInterface, Assembly assembly)
    {
        var registrations = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == openInterface)
                .Select(contract => (Contract: contract, Implementation: type)));

        foreach (var (contract, implementation) in registrations)
        {
            services.AddScoped(contract, implementation);
        }

        return services;
    }
}
