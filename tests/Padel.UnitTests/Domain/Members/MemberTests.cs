using AwesomeAssertions;
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
    public void Register_Should_CreateActiveMemberAndRaiseMemberRegistered()
    {
        var member = CreateMember();

        member.Status.Should().Be(MemberStatus.Active);
        member.CanBook.Should().BeTrue();
        member.RegisteredOn.Should().Be(RegisteredOn);
        var registered = (MemberRegistered)member.DomainEvents.Single();
        registered.MemberId.Should().Be(member.Id);
    }

    [Test]
    public void Register_Should_NormalizePhoneToDigitsOnly()
    {
        var member = CreateMember("+31 (0)10 123 4567");

        member.Phone.Should().Be("310101234567");
    }

    [Test]
    public void Given_BlankPhone_Should_StoreNull()
    {
        var member = CreateMember("  ");

        member.Phone.Should().BeNull();
    }

    [Test]
    [Arguments("1234567")]
    [Arguments("1234567890123456")]
    public void Given_PhoneWithInvalidDigitCount_Should_Fail(string phone)
    {
        Register(phone).ShouldFail().Is<ValidationError>();
    }

    [Test]
    public void Suspend_Should_BlockBookingStoreReasonAndRaiseMemberSuspended()
    {
        var member = CreateMember();
        member.ClearDomainEvents();

        member.Suspend("  unpaid fees ");

        member.Status.Should().Be(MemberStatus.Suspended);
        member.CanBook.Should().BeFalse();
        member.SuspensionReason.Should().Be("unpaid fees");
        var suspended = (MemberSuspended)member.DomainEvents.Single();
        suspended.MemberId.Should().Be(member.Id);
        suspended.Reason.Should().Be("unpaid fees");
    }

    [Test]
    public void Given_SuspendedMember_Suspend_Should_NotRaiseSecondEvent()
    {
        var member = CreateMember();
        member.Suspend("first");
        member.ClearDomainEvents();

        member.Suspend("second");

        member.DomainEvents.Should().BeEmpty();
        member.SuspensionReason.Should().Be("first");
    }

    [Test]
    public void Given_SuspendedMember_Reactivate_Should_ClearReasonAndRaiseMemberReactivated()
    {
        var member = CreateMember();
        member.Suspend("unpaid fees");
        member.ClearDomainEvents();

        member.Reactivate();

        member.Status.Should().Be(MemberStatus.Active);
        member.SuspensionReason.Should().BeNull();
        member.DomainEvents.Single().Should().BeOfType<MemberReactivated>();
    }

    [Test]
    public void Given_ActiveMember_Reactivate_Should_NotRaiseEvent()
    {
        var member = CreateMember();
        member.ClearDomainEvents();

        member.Reactivate();

        member.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void UpdateProfile_Should_ReplaceNameEmailAndPhone()
    {
        var member = CreateMember();

        member.UpdateProfile(FullName.Create("Jan", "Jansen").ShouldSucceed(), Email.Create("jan@example.com").ShouldSucceed(), "010-1234567");

        member.Name.Should().Be(FullName.Create("Jan", "Jansen").ShouldSucceed());
        member.Email.Should().Be(Email.Create("jan@example.com").ShouldSucceed());
        member.Phone.Should().Be("0101234567");
    }
}
