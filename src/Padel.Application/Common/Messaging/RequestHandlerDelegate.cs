using Padel.Domain.Common;

namespace Padel.Application.Common.Messaging;

public delegate Task<Result<TValue>> RequestHandlerDelegate<TValue>() where TValue : notnull;
