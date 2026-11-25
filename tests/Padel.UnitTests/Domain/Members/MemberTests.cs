using AwesomeAssertions;
using Padel.Domain.Common;
using Padel.Domain.Members;
using Padel.UnitTests.Builders;

namespace Padel.UnitTests.Domain.Members;

public class MemberTests
{
    private static readonly DateTimeOffset RegisteredOn = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    // Tests of the registration itself call the factory; the others start from a generated member.
    private static Result<Member> Register(string? phone = null) =>
        Member.Register(FullName.Create("Gert-Jan", "De Vries").ShouldSucceed(), Email.Create("gertjan@example.com").ShouldSucceed(), phone, RegisteredOn);

    [Test]
    public void Register_ShouldCreateActiveMemberAndRaiseMemberRegistered()
    {
        var member = Register().ShouldSucceed();

        member.Status.Should().Be(MemberStatus.Active);
        member.CanBook.Should().BeTrue();
        member.RegisteredOn.Should().Be(RegisteredOn);
        var registered = (MemberRegistered)member.DomainEvents.Single();
        registered.MemberId.Should().Be(member.Id);
    }

    [Test]
    public void Register_ShouldNormalizePhoneToDigitsOnly()
    {
        var member = Register("+31 (0)10 123 4567").ShouldSucceed();

        member.Phone.Should().Be("310101234567");
    }

    [Test]
    public void Register_WhenBlankPhone_ShouldStoreNull()
    {
        var member = Register("  ").ShouldSucceed();

        member.Phone.Should().BeNull();
    }

    [Test]
    [Arguments("1234567")]
    [Arguments("1234567890123456")]
    public void Register_WhenPhoneWithInvalidDigitCount_ShouldFail(string phone)
    {
        Register(phone).ShouldFail().Is<ValidationError>();
    }

    [Test]
    public void LinkIdentity_ShouldLinkOnceAndAllowTheSameAccountAgain()
    {
        var member = MemberBuilder.New().Generate();

        member.LinkIdentity("account-1").ShouldSucceed();
        member.LinkIdentity("account-1").ShouldSucceed();

        member.IdentityId.Should().Be("account-1");
        member.LinkIdentity("account-2").ShouldFail().Is<ConflictError>().Code.Should().Be("Member.AlreadyLinked");
    }

    [Test]
    [Arguments("010-123 4567 ext")]
    [Arguments("010+1234567")]
    [Arguments("06.12345678")]
    public void Register_WhenPhoneWithInvalidCharacters_ShouldFail(string phone)
    {
        Register(phone).ShouldFail("may only contain digits").Is<ValidationError>();
    }

    [Test]
    [Arguments("+31 (0)10-123 4567")]
    [Arguments("0612345678")]
    public void Register_WhenPhoneInAllowedFormat_ShouldSucceed(string phone)
    {
        Register(phone).ShouldSucceed();
    }

    [Test]
    public void Suspend_ShouldBlockBookingStoreReasonAndRaiseMemberSuspended()
    {
        var member = MemberBuilder.New().Generate();

        member.Suspend("  unpaid fees ");

        member.Status.Should().Be(MemberStatus.Suspended);
        member.CanBook.Should().BeFalse();
        member.SuspensionReason.Should().Be("unpaid fees");
        var suspended = (MemberSuspended)member.DomainEvents.Single();
        suspended.MemberId.Should().Be(member.Id);
        suspended.Reason.Should().Be("unpaid fees");
    }

    [Test]
    public void Suspend_GivenSuspendedMember_ShouldNotRaiseSecondEvent()
    {
        var member = MemberBuilder.New().Suspended("first").Generate();

        member.Suspend("second");

        member.DomainEvents.Should().BeEmpty();
        member.SuspensionReason.Should().Be("first");
    }

    [Test]
    public void Reactivate_GivenSuspendedMember_ShouldClearReasonAndRaiseMemberReactivated()
    {
        var member = MemberBuilder.New().Suspended("unpaid fees").Generate();

        member.Reactivate();

        member.Status.Should().Be(MemberStatus.Active);
        member.SuspensionReason.Should().BeNull();
        member.DomainEvents.Single().Should().BeOfType<MemberReactivated>();
    }

    [Test]
    public void Reactivate_GivenActiveMember_ShouldNotRaiseEvent()
    {
        var member = MemberBuilder.New().Generate();

        member.Reactivate();

        member.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void UpdateProfile_ShouldReplaceNameEmailAndPhone()
    {
        var member = MemberBuilder.New().Generate();

        member.UpdateProfile(FullName.Create("Jan", "Jansen").ShouldSucceed(), Email.Create("jan@example.com").ShouldSucceed(), "010-1234567");

        member.Name.Should().Be(FullName.Create("Jan", "Jansen").ShouldSucceed());
        member.Email.Should().Be(Email.Create("jan@example.com").ShouldSucceed());
        member.Phone.Should().Be("0101234567");
    }
}
