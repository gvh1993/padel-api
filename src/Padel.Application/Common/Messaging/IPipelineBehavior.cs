namespace Padel.Application.Common.Messaging;

public interface IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
