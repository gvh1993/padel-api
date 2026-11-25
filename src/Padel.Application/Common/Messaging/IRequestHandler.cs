using Padel.Domain.Common;

namespace Padel.Application.Common.Messaging;

public interface IRequestHandler<TRequest, TValue> where TRequest : IRequest<TValue>
    where TValue : notnull
{
    Task<Result<TValue>> Handle(TRequest request, CancellationToken cancellationToken);
}
