using Padel.Domain.Common;

namespace Padel.Domain.Courts;

public static class CourtErrors
{
    public static readonly Error RetiredCannotChangeMaintenance = Error.BusinessRule(
        "Court.Retired",
        "A retired court cannot change maintenance state.");

    public static Error Required(string label) =>
        Error.Validation($"Court.{label.Replace(" ", string.Empty)}Required", $"{label} is required.");

    public static Error TooLong(string label) =>
        Error.Validation($"Court.{label.Replace(" ", string.Empty)}TooLong", $"{label} cannot be longer than {Court.MaxLength} characters.");
}
