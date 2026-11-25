using Padel.Domain.Common;

namespace Padel.Domain.Members;

public class Member : EntityWithDomainEvents
{
    public const int PhoneMinDigits = 8;
    public const int PhoneMaxDigits = 15;

    private Member(FullName name, Email email, string? phone, DateTimeOffset registeredOn)
    {
        Name = name;
        Email = email;
        Phone = phone;
        Status = MemberStatus.Active;
        RegisteredOn = registeredOn;

        AddDomainEvent(new MemberRegistered(Id));
    }

    private Member()
    {
    }

    public FullName Name { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public string? Phone { get; private set; }
    public MemberStatus Status { get; private set; }
    public string? SuspensionReason { get; private set; }
    public DateTimeOffset RegisteredOn { get; private set; }

    /// <summary>The member's account at the identity provider (the token's <c>sub</c>), once linked.</summary>
    public string? IdentityId { get; private set; }

    public bool CanBook => Status == MemberStatus.Active;

    public static Result<Member> Register(FullName name, Email email, string? phone, DateTimeOffset registeredOn)
    {
        if (ValidatePhone(phone) is Error error)
        {
            return error;
        }

        return new Member(name, email, DigitsOf(phone), registeredOn);
    }

    public Result UpdateProfile(FullName name, Email email, string? phone)
    {
        if (ValidatePhone(phone) is Error error)
        {
            return error;
        }

        Name = name;
        Email = email;
        Phone = DigitsOf(phone);
        return Success.Instance;
    }

    /// <summary>Suspends the member. Suspending a suspended member has no effect.</summary>
    public Result Suspend(string? reason = null)
    {
        if (Status == MemberStatus.Suspended)
        {
            return Success.Instance;
        }

        var suspensionReason = Reason.Create(reason);
        if (suspensionReason is not Reason validReason)
        {
            return suspensionReason.Error;
        }

        Status = MemberStatus.Suspended;
        SuspensionReason = validReason.Text;
        AddDomainEvent(new MemberSuspended(Id, SuspensionReason));
        return Success.Instance;
    }

    /// <summary>
    /// Links the member to an account at the identity provider. Linking the same account again has no
    /// effect; a member that is linked to another account cannot be taken over.
    /// </summary>
    public Result LinkIdentity(string identityId)
    {
        if (IdentityId == identityId)
        {
            return Success.Instance;
        }

        if (IdentityId is not null)
        {
            return MemberErrors.AlreadyLinked;
        }

        IdentityId = identityId;
        AddDomainEvent(new MemberIdentityLinked(Id, identityId));
        return Success.Instance;
    }

    /// <summary>Reactivates the member. Reactivating an active member has no effect.</summary>
    public void Reactivate()
    {
        if (Status == MemberStatus.Active)
        {
            return;
        }

        Status = MemberStatus.Active;
        SuspensionReason = null;
        AddDomainEvent(new MemberReactivated(Id));
    }

    /// <summary>
    /// A phone number is optional. When given, it may only contain digits, spaces, '-', '(' and ')',
    /// optionally after a leading '+', and must have a valid number of digits.
    /// </summary>
    public static Result ValidatePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return Success.Instance;
        }

        var trimmed = phone.Trim();
        var number = trimmed.StartsWith('+') ? trimmed[1..] : trimmed;
        var allowedCharacters = number.All(c => char.IsDigit(c) || c is ' ' or '-' or '(' or ')');

        if (!allowedCharacters || DigitsOf(phone) is { Length: < PhoneMinDigits or > PhoneMaxDigits })
        {
            return MemberErrors.InvalidPhone;
        }

        return Success.Instance;
    }

    private static string? DigitsOf(string? phone) =>
        string.IsNullOrWhiteSpace(phone) ? null : new string(phone.Where(char.IsDigit).ToArray());
}
