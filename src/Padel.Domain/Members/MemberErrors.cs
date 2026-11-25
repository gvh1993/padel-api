using Padel.Domain.Common;

namespace Padel.Domain.Members;

public static class MemberErrors
{
    public static readonly Error InvalidPhone = Error.Validation(
        "Member.InvalidPhone",
        $"Phone number may only contain digits, spaces, '+', '-', '(' and ')' and must contain between {Member.PhoneMinDigits} and {Member.PhoneMaxDigits} digits.");

    public static Error EmailInUse(Email email) =>
        Error.Conflict("Member.EmailInUse", $"A member with email '{email}' already exists.");
}
