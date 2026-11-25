using FluentValidation;
using Padel.Domain.Common;
using Padel.Domain.Courts;
using Padel.Domain.Members;

namespace Padel.Application.Common.Validation;

/// <summary>
/// Reusable rules that mirror the domain invariants, so callers get all errors at once as a 400.
/// </summary>
public static class RuleBuilderExtensions
{
    public static IRuleBuilderOptions<T, string> ValidPersonName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(FullName.MaxLength);

    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(Email.MaxLength).EmailAddress();

    public static IRuleBuilderOptions<T, string?> ValidPhone<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(phone => string.IsNullOrWhiteSpace(phone) || IsValidPhone(phone))
            .WithMessage($"'{{PropertyName}}' may only contain digits, spaces, '+', '-', '(' and ')' and must contain between {Member.PhoneMinDigits} and {Member.PhoneMaxDigits} digits.");

    public static IRuleBuilderOptions<T, string> ValidCourtText<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(Court.MaxLength);

    public static IRuleBuilderOptions<T, string?> ValidReason<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(Reason.MaxLength);

    private static bool IsValidPhone(string phone)
    {
        var trimmed = phone.Trim();
        var allowedCharacters = trimmed.All(c => char.IsDigit(c) || c is ' ' or '-' or '(' or ')')
            || (trimmed.StartsWith('+') && trimmed[1..].All(c => char.IsDigit(c) || c is ' ' or '-' or '(' or ')'));
        var digits = trimmed.Count(char.IsDigit);

        return allowedCharacters && digits is >= Member.PhoneMinDigits and <= Member.PhoneMaxDigits;
    }
}
