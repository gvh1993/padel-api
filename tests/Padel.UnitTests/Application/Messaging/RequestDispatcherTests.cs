using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Padel.Application.Common.Messaging;
using Padel.Domain.Common;

namespace Padel.UnitTests.Application.Messaging;

public class RequestDispatcherTests
{
    public sealed record Ping(string Value) : IRequest<string>;

    public sealed class CallLog
    {
        public List<string> Entries { get; } = [];
    }

    public sealed class PingHandler(CallLog log) : IRequestHandler<Ping, string>
    {
        public Task<Result<string>> Handle(Ping request, CancellationToken cancellationToken)
        {
            log.Entries.Add("handler");
            return Task.FromResult<Result<string>>($"pong:{request.Value}");
        }
    }

    public sealed class OuterBehavior(CallLog log) : IPipelineBehavior<Ping, string>
    {
        public async Task<Result<string>> Handle(Ping request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            log.Entries.Add("outer:before");
            var response = await next();
            log.Entries.Add("outer:after");
            return response;
        }
    }

    public sealed class InnerBehavior(CallLog log) : IPipelineBehavior<Ping, string>
    {
        public async Task<Result<string>> Handle(Ping request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            log.Entries.Add("inner:before");
            var response = await next();
            log.Entries.Add("inner:after");
            return response;
        }
    }

    public sealed class ShortCircuitBehavior : IPipelineBehavior<Ping, string>
    {
        public static readonly Error Rejected = Error.Forbidden("Ping.Rejected", "No pings allowed.");

        public Task<Result<string>> Handle(Ping request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken) =>
            Task.FromResult<Result<string>>(Rejected);
    }

    [Test]
    public async Task Send_Should_RunBehaviorsInRegistrationOrderAroundTheHandler()
    {
        var services = new ServiceCollection()
            .AddSingleton<CallLog>()
            .AddScoped<IRequestHandler<Ping, string>, PingHandler>()
            .AddScoped<IPipelineBehavior<Ping, string>, OuterBehavior>()
            .AddScoped<IPipelineBehavior<Ping, string>, InnerBehavior>()
            .AddScoped<IRequestDispatcher, RequestDispatcher>();
        await using var provider = services.BuildServiceProvider();

        var response = await provider.GetRequiredService<IRequestDispatcher>().Send<Ping, string>(new Ping("x")).ShouldSucceed();

        response.Should().Be("pong:x");
        provider.GetRequiredService<CallLog>().Entries.Should().Equal(new[] { "outer:before", "inner:before", "handler", "inner:after", "outer:after" });
    }

    [Test]
    public async Task Given_BehaviorThatFails_Send_Should_ReturnItsErrorWithoutCallingTheHandler()
    {
        var services = new ServiceCollection()
            .AddSingleton<CallLog>()
            .AddScoped<IRequestHandler<Ping, string>, PingHandler>()
            .AddScoped<IPipelineBehavior<Ping, string>, ShortCircuitBehavior>()
            .AddScoped<IRequestDispatcher, RequestDispatcher>();
        await using var provider = services.BuildServiceProvider();

        var error = await provider.GetRequiredService<IRequestDispatcher>().Send<Ping, string>(new Ping("x")).ShouldFail();

        error.Should().Be(ShortCircuitBehavior.Rejected);
        provider.GetRequiredService<CallLog>().Entries.Should().BeEmpty();
    }

    [Test]
    public async Task Given_NoHandler_Send_Should_ThrowInvalidOperationException()
    {
        await using var provider = new ServiceCollection()
            .AddScoped<IRequestDispatcher, RequestDispatcher>()
            .BuildServiceProvider();

        await FluentActions.Awaiting(() =>
            provider.GetRequiredService<IRequestDispatcher>().Send<Ping, string>(new Ping("x"))).Should().ThrowAsync<InvalidOperationException>();
    }
}
