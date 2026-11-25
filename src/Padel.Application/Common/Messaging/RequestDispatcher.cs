using Microsoft.Extensions.DependencyInjection;

namespace Padel.Application.Common.Messaging;

public sealed class RequestDispatcher(IServiceProvider provider) : IRequestDispatcher
{
    public async Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>
    {
        var handler = provider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = provider.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToList();

        RequestHandlerDelegate<TResponse> pipeline = () => handler.Handle(request, cancellationToken);

        foreach (var behavior in behaviors.AsEnumerable().Reverse())
        {
            var current = pipeline;
            pipeline = () => behavior.Handle(request, current, cancellationToken);
        }

        return await pipeline();
    }
}
