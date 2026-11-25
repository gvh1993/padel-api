using AwesomeAssertions;
using Padel.Domain.Common;

namespace Padel.UnitTests;

/// <summary>
/// Assertions for result unions, with the error in the message when an expectation fails.
/// <code>
/// var court = Court.Create("A", "glass").ShouldSucceed();
/// booking.Cancel().ShouldFail("already cancelled");
/// await handler.Handle(command, CancellationToken.None).ShouldFail().Is&lt;NotFoundError&gt;();
/// </code>
/// </summary>
internal static class ResultAssertions
{
    public static async Task<T> ShouldSucceed<T>(this Task<Result<T>> result) where T : notnull =>
        (await result).ShouldSucceed();

    public static T ShouldSucceed<T>(this Result<T> result) where T : notnull
    {
        result.Value.Should().BeAssignableTo<T>("the result should be a success, but it {0}", Describe(result.Value));
        return (T)result.Value!;
    }

    public static void ShouldSucceed(this Result result) =>
        result.Value.Should().BeOfType<Success>("the result should be a success, but it {0}", Describe(result.Value));

    public static async Task<Error> ShouldFail<T>(this Task<Result<T>> result, string? descriptionContaining = null)
        where T : notnull =>
        (await result).ShouldFail(descriptionContaining);

    /// <summary>Asserts a failure, optionally with a description containing a text, and returns the error.</summary>
    public static Error ShouldFail<T>(this Result<T> result, string? descriptionContaining = null) where T : notnull =>
        ShouldBeError(result.Value, descriptionContaining);

    public static Error ShouldFail(this Result result, string? descriptionContaining = null) =>
        ShouldBeError(result.Value, descriptionContaining);

    /// <summary>Asserts the kind of error and returns it.</summary>
    public static TError Is<TError>(this Error error) where TError : IError
    {
        error.Value.Should().BeOfType<TError>("the error should be a {0}, but it is {1}", typeof(TError).Name, error.Code);
        return (TError)error.Value!;
    }

    public static async Task<TError> Is<TError>(this Task<Error> error) where TError : IError => (await error).Is<TError>();

    private static Error ShouldBeError(object? value, string? descriptionContaining)
    {
        value.Should().BeOfType<Error>("the result should be a failure");
        var error = (Error)value!;

        if (descriptionContaining is not null)
        {
            error.Description.Should().Contain(descriptionContaining);
        }

        return error;
    }

    private static string Describe(object? value) =>
        value is Error error ? $"failed with {error.Code}: {error.Description}" : "succeeded";
}
