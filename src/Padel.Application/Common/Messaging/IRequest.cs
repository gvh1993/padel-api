namespace Padel.Application.Common.Messaging;

/// <summary>A command or query whose handler returns a <c>Result&lt;TValue&gt;</c>.</summary>
public interface IRequest<TValue> where TValue : notnull
{
}
