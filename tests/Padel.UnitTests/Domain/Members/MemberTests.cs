using Padel.Domain.Common;
using Padel.Domain.Members;

namespace Padel.UnitTests.Domain.Members;

public class MemberTests
{
    private static readonly DateTimeOffset RegisteredOn = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static Result<Member> Register(string? phone = null) =>
        Member.Register(FullName.Create("Gert-Jan", "De Vries").ShouldSucceed(), Email.Create("gertjan@example.com").ShouldSucceed(), phone, RegisteredOn);

    private static Member CreateMember(string? phone = null) => Register(phone).ShouldSucceed();

    [Test]
    public async Task Register_Should_CreateActiveMemberAndRaiseMemberRegistered()
    {
        var member = CreateMember();

        await Assert.That(member.Status).IsEqualTo(MemberStatus.Active);
        await Assert.That(member.CanBook).IsTrue();
        await Assert.That(member.RegisteredOn).IsEqualTo(RegisteredOn);
        var registered = (MemberRegistered)member.DomainEvents.Single();
        await Assert.That(registered.MemberId).IsEqualTo(member.Id);
    }

    [Test]
    public async Task Register_Should_NormalizePhoneToDigitsOnly()
    {
        var member = CreateMember("+31 (0)10 123 4567");

        await Assert.That(member.Phone).IsEqualTo("310101234567");
    }

    [Test]
    public async Task Given_BlankPhone_Should_StoreNull()
    {
        var member = CreateMember("  ");

        await Assert.That(member.Phone).IsNull();
    }

    [Test]
    [Arguments("1234567")]
    [Arguments("1234567890123456")]
    public async Task Given_PhoneWithInvalidDigitCount_Should_Fail(string phone)
    {
        await Register(phone).ShouldFail().Is<ValidationError>();
    }

    [Test]
    public async Task Suspend_Should_BlockBookingStoreReasonAndRaiseMemberSuspended()
    {
        var member = CreateMember();
        member.ClearDomainEvents();

        member.Suspend("  unpaid fees ");

        await Assert.That(member.Status).IsEqualTo(MemberStatus.Suspended);
        await Assert.That(member.CanBook).IsFalse();
        await Assert.That(member.SuspensionReason).IsEqualTo("unpaid fees");
        var suspended = (MemberSuspended)member.DomainEvents.Single();
        await Assert.That(suspended.MemberId).IsEqualTo(member.Id);
        await Assert.That(suspended.Reason).IsEqualTo("unpaid fees");
    }

    [Test]
    public async Task Given_SuspendedMember_Suspend_Should_NotRaiseSecondEvent()
    {
        var member = CreateMember();
        member.Suspend("first");
        member.ClearDomainEvents();

        member.Suspend("second");

        await Assert.That(member.DomainEvents).IsEmpty();
        await Assert.That(member.SuspensionReason).IsEqualTo("first");
    }

    [Test]
    public async Task Given_SuspendedMember_Reactivate_Should_ClearReasonAndRaiseMemberReactivated()
    {
        var member = CreateMember();
        member.Suspend("unpaid fees");
        member.ClearDomainEvents();

        member.Reactivate();

        await Assert.That(member.Status).IsEqualTo(MemberStatus.Active);
        await Assert.That(member.SuspensionReason).IsNull();
        await Assert.That(member.DomainEvents.Single()).IsTypeOf<MemberReactivated>();
    }

    [Test]
    public async Task Given_ActiveMember_Reactivate_Should_NotRaiseEvent()
    {
        var member = CreateMember();
        member.ClearDomainEvents();

        member.Reactivate();

        await Assert.That(member.DomainEvents).IsEmpty();
    }

    [Test]
    public async Task UpdateProfile_Should_ReplaceNameEmailAndPhone()
    {
        var member = CreateMember();

        member.UpdateProfile(FullName.Create("Jan", "Jansen").ShouldSucceed(), Email.Create("jan@example.com").ShouldSucceed(), "010-1234567");

        await Assert.That(member.Name).IsEqualTo(FullName.Create("Jan", "Jansen").ShouldSucceed());
        await Assert.That(member.Email).IsEqualTo(Email.Create("jan@example.com").ShouldSucceed());
        await Assert.That(member.Phone).IsEqualTo("0101234567");
    }
}
