using Padel.Domain.Common;

namespace Padel.Application.Common.Messaging;

public interface IPipelineBehavior<TRequest, TValue> where TRequest : notnull
    where TValue : notnull
{
    Task<Result<TValue>> Handle(TRequest request, RequestHandlerDelegate<TValue> next, CancellationToken cancellationToken);
}
