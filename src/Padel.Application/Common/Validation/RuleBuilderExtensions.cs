using FluentValidation;
using Padel.Domain.Common;
using Padel.Domain.Courts;
using Padel.Domain.Members;

namespace Padel.Application.Common.Validation;

/// <summary>
/// Request rules that ask the domain: a value is valid when the domain would accept it, and the
/// domain's error becomes the field's message. The rules themselves live only in the domain;
/// validators exist so a client gets every invalid field at once, as a 400.
/// </summary>
public static class RuleBuilderExtensions
{
    public static IRuleBuilderOptionsConditions<T, TProperty> AcceptedBy<T, TProperty, TValue>(
        this IRuleBuilder<T, TProperty> rule,
        Func<TProperty, Result<TValue>> create)
        where TValue : notnull =>
        rule.Custom((value, context) =>
        {
            if (create(value) is Error error)
            {
                context.AddFailure(error.Description);
            }
        });

    public static IRuleBuilderOptionsConditions<T, TProperty> AcceptedBy<T, TProperty>(
        this IRuleBuilder<T, TProperty> rule,
        Func<TProperty, Result> validate) =>
        rule.Custom((value, context) =>
        {
            if (validate(value) is Error error)
            {
                context.AddFailure(error.Description);
            }
        });

    public static IRuleBuilderOptionsConditions<T, string> ValidFirstName<T>(this IRuleBuilder<T, string> rule) =>
        rule.AcceptedBy(FullName.CreateFirstName);

    public static IRuleBuilderOptionsConditions<T, string> ValidLastName<T>(this IRuleBuilder<T, string> rule) =>
        rule.AcceptedBy(FullName.CreateLastName);

    public static IRuleBuilderOptionsConditions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.AcceptedBy(Email.Create);

    public static IRuleBuilderOptionsConditions<T, string?> ValidPhone<T>(this IRuleBuilder<T, string?> rule) =>
        rule.AcceptedBy(Member.ValidatePhone);

    public static IRuleBuilderOptionsConditions<T, string> ValidCourtName<T>(this IRuleBuilder<T, string> rule) =>
        rule.AcceptedBy(Court.CreateName);

    public static IRuleBuilderOptionsConditions<T, string> ValidSurface<T>(this IRuleBuilder<T, string> rule) =>
        rule.AcceptedBy(Court.CreateSurface);

    public static IRuleBuilderOptionsConditions<T, string?> ValidReason<T>(this IRuleBuilder<T, string?> rule) =>
        rule.AcceptedBy(Reason.Create);
}
