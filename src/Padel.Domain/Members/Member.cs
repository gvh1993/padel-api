using Padel.Domain.Common;

namespace Padel.Domain.Members;

public class Member : EntityWithDomainEvents
{
    public const int PhoneMinDigits = 8;
    public const int PhoneMaxDigits = 15;

    public FullName Name { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public string? Phone { get; private set; }
    public MemberStatus Status { get; private set; }
    public string? SuspensionReason { get; private set; }
    public DateTimeOffset RegisteredOn { get; private set; }

    public Member(FullName name, Email email, string? phone, DateTimeOffset registeredOn)
    {
        Name = name;
        Email = email;
        Phone = NormalizePhone(phone);
        Status = MemberStatus.Active;
        RegisteredOn = registeredOn;

        AddDomainEvent(new MemberRegistered(Id));
    }

    private Member()
    {
    }

    public bool CanBook => Status == MemberStatus.Active;

    public void UpdateProfile(FullName name, Email email, string? phone)
    {
        Name = name;
        Email = email;
        Phone = NormalizePhone(phone);
    }

    public void Suspend(string? reason = null)
    {
        if (Status == MemberStatus.Suspended)
        {
            return;
        }

        Status = MemberStatus.Suspended;
        SuspensionReason = Reason.Normalize(reason);
        AddDomainEvent(new MemberSuspended(Id, SuspensionReason));
    }

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

    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var cleaned = new string(phone.Where(char.IsDigit).ToArray());
        if (cleaned.Length is < PhoneMinDigits or > PhoneMaxDigits)
        {
            throw new DomainException($"Phone number must contain between {PhoneMinDigits} and {PhoneMaxDigits} digits.");
        }

        return cleaned;
    }
}
