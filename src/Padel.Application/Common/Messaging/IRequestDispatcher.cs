using Padel.Domain.Common;

namespace Padel.Application.Common.Messaging;

public interface IRequestDispatcher
{
    Task<Result<TValue>> Send<TRequest, TValue>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest<TValue>
        where TValue : notnull;
}
