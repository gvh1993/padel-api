using System.Diagnostics;
using Padel.Domain.Common;

namespace Padel.UnitTests;

/// <summary>
/// Assertions for result unions, with the error in the message when an expectation fails.
/// <code>
/// var court = Court.Create("A", "glass").ShouldSucceed();
/// await handler.Handle(command, CancellationToken.None).ShouldFail().Is&lt;NotFoundError&gt;();
/// </code>
/// </summary>
internal static class ResultAssertions
{
    public static async Task<T> ShouldSucceed<T>(this Task<Result<T>> result) where T : notnull =>
        (await result).ShouldSucceed();

    public static T ShouldSucceed<T>(this Result<T> result) where T : notnull => result switch
    {
        T value => value,
        Error error => Fail<T>(error),
    };

    public static void ShouldSucceed(this Result result)
    {
        if (result is Error error)
        {
            Fail<Success>(error);
        }
    }

    public static async Task<Error> ShouldFail<T>(this Task<Result<T>> result, string? descriptionContaining = null)
        where T : notnull =>
        await (await result).ShouldFail(descriptionContaining);

    /// <summary>Asserts a failure, optionally with a description containing a text, and returns the error.</summary>
    public static Task<Error> ShouldFail<T>(this Result<T> result, string? descriptionContaining = null) where T : notnull =>
        result switch
        {
            Error error => error.ShouldDescribe(descriptionContaining),
            _ => Task.FromResult(Fail<Error>("Expected a failure, but the result was successful.")),
        };

    public static Task<Error> ShouldFail(this Result result, string? descriptionContaining = null) => result switch
    {
        Error error => error.ShouldDescribe(descriptionContaining),
        _ => Task.FromResult(Fail<Error>("Expected a failure, but the result was successful.")),
    };

    /// <summary>Asserts the kind of error and returns it.</summary>
    public static async Task<TError> Is<TError>(this Task<Error> error) where TError : IError
    {
        var value = (await error).Value;
        await Assert.That(value).IsTypeOf<TError>();
        return (TError)value!;
    }

    private static async Task<Error> ShouldDescribe(this Error error, string? descriptionContaining)
    {
        if (descriptionContaining is not null)
        {
            await Assert.That(error.Description).Contains(descriptionContaining);
        }

        return error;
    }

    private static T Fail<T>(Error error) => Fail<T>($"Expected success, but failed with {error.Code}: {error.Description}");

    private static T Fail<T>(string message)
    {
        Assert.Fail(message);
        throw new UnreachableException();
    }
}
