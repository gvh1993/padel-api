namespace Padel.Domain.Common;

/// <summary>
/// The outcome of an operation that returns a <typeparamref name="T"/>: the value or an <see cref="Common.Error"/>.
/// <code>
/// var email = Email.Create(input);
/// if (email is not Email validEmail)
/// {
///     return email.Error;
/// }
/// </code>
/// </summary>
public union Result<T>(T, Error) where T : notnull
{
    /// <summary>The error of a failed result; use it after checking that the result is not a <typeparamref name="T"/>.</summary>
    public Error Error => Value is Error error ? error : throw new InvalidOperationException("A successful result has no error.");
}
