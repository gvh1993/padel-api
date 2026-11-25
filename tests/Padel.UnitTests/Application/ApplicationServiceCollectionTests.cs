using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Padel.Application;
using Padel.Application.Common;
using Padel.Application.Common.Behaviors;
using Padel.Application.Common.Messaging;
using Padel.Application.Features.Bookings;
using Padel.Application.Features.Courts;
using Padel.Application.Features.Members;
using Padel.Domain.Common;
using Padel.Domain.Members;
using Padel.UnitTests.Fakes;

namespace Padel.UnitTests.Application;

public class ApplicationServiceCollectionTests
{
    public sealed class RecordingMemberRegisteredHandler : DomainEventHandler<MemberRegistered>
    {
        public List<MemberRegistered> Received { get; } = [];

        public override Task Handle(MemberRegistered domainEvent, CancellationToken cancellationToken)
        {
            Received.Add(domainEvent);
            return Task.CompletedTask;
        }
    }

    private static ServiceProvider BuildProvider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddScoped<IMemberRepository, InMemoryMemberRepository>();
        services.AddScoped<ICourtRepository, InMemoryCourtRepository>();
        services.AddScoped<IBookingRepository, InMemoryBookingRepository>();
        services.AddScoped<IUnitOfWork>(_ => new FakeUnitOfWork());
        services.AddScoped<ICurrentUser>(_ => FakeCurrentUser.Admin());
        configure?.Invoke(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Test]
    public async Task AddApplication_Should_RegisterAHandlerForEveryRequest()
    {
        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();

        var requestTypes = typeof(ApplicationServiceCollection).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequest<>))
                .Select(contract => (Request: type, Response: contract.GetGenericArguments()[0])))
            .ToList();

        requestTypes.Count.Should().BeGreaterThan(15);

        foreach (var (request, response) in requestTypes)
        {
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request, response);
            scope.ServiceProvider.GetService(handlerType).Should().NotBeNull();
        }
    }

    [Test]
    public async Task AddApplication_Should_RegisterTelemetryBeforeValidationBehavior()
    {
        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();

        var behaviors = scope.ServiceProvider.GetServices<IPipelineBehavior<GetMemberQuery, MemberDto>>()
            .Select(behavior => behavior.GetType().GetGenericTypeDefinition());

        behaviors.Should().Equal(new[] { typeof(TelemetryBehavior<,>), typeof(ValidationBehavior<,>) });
    }

    [Test]
    public async Task Given_InvalidCommand_Dispatcher_Should_FailWithValidationErrorsFromPipeline()
    {
        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IRequestDispatcher>();

        var error = await dispatcher.Send<RegisterMemberCommand, MemberDto>(
            new RegisterMemberCommand { FirstName = "", LastName = "", Email = "" }).ShouldFail().Is<ValidationError>();

        error.FieldErrors.Keys.Should().Contain("Email");
    }

    [Test]
    public async Task AddDomainEventHandlersFromAssembly_Should_LetThePublisherReachTheHandler()
    {
        await using var provider = BuildProvider(services => services.AddDomainEventHandlersFromAssembly(typeof(ApplicationServiceCollectionTests).Assembly));
        await using var scope = provider.CreateAsyncScope();
        var domainEvent = new MemberRegistered(Guid.NewGuid());

        await scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>().PublishAsync(domainEvent);

        var handler = (RecordingMemberRegisteredHandler)scope.ServiceProvider.GetRequiredService<IDomainEventHandler<MemberRegistered>>();
        handler.Received.Should().BeEquivalentTo(new[] { domainEvent });
    }

    [Test]
    public async Task Given_NoHandlers_Publisher_Should_IgnoreTheEvent()
    {
        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();

        await FluentActions.Awaiting(() => scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>()
            .PublishAsync(new MemberSuspended(Guid.NewGuid(), null))).Should().NotThrowAsync();
    }

    [Test]
    public async Task AddApplication_Should_RegisterSystemTimeProvider()
    {
        await using var provider = BuildProvider();

        provider.GetRequiredService<TimeProvider>().Should().BeSameAs(TimeProvider.System);
    }
}
