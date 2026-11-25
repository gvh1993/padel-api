using Microsoft.Extensions.DependencyInjection;
using Padel.Domain.Common;

namespace Padel.Application.Common.Messaging;

public sealed class RequestDispatcher(IServiceProvider provider) : IRequestDispatcher
{
    public async Task<Result<TValue>> Send<TRequest, TValue>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest<TValue>
        where TValue : notnull
    {
        var handler = provider.GetRequiredService<IRequestHandler<TRequest, TValue>>();
        var behaviors = provider.GetServices<IPipelineBehavior<TRequest, TValue>>().ToList();

        RequestHandlerDelegate<TValue> pipeline = () => handler.Handle(request, cancellationToken);

        foreach (var behavior in behaviors.AsEnumerable().Reverse())
        {
            var current = pipeline;
            pipeline = () => behavior.Handle(request, current, cancellationToken);
        }

        return await pipeline();
    }
}
