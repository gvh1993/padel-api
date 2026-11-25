namespace Padel.Domain.Common;

/// <summary>
/// The outcome of an operation without a value: <see cref="Success"/> or an <see cref="Common.Error"/>.
/// Expected failures (invalid input, broken business rules) are results, not exceptions:
/// <c>if (court.SetMaintenance(true) is Error error) return error;</c>
/// </summary>
public union Result(Success, Error);
