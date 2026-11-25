using AwesomeAssertions;
using Padel.Application.Common;
using Padel.Domain.Common;
using Padel.UnitTests.Fakes;

namespace Padel.UnitTests.Application.Common;

public class CurrentUserExtensionsTests
{
    private static readonly Guid MemberId = Guid.NewGuid();

    [Test]
    public void Given_Admin_CanAccessMember_Should_Succeed()
    {
        FakeCurrentUser.Admin().CanAccessMember(MemberId).ShouldSucceed();
    }

    [Test]
    public void Given_SameMember_CanAccessMember_Should_Succeed()
    {
        FakeCurrentUser.ForMember(MemberId).CanAccessMember(MemberId).ShouldSucceed();
    }

    [Test]
    public void Given_OtherMember_CanAccessMember_Should_FailWithForbidden()
    {
        FakeCurrentUser.ForMember(Guid.NewGuid()).CanAccessMember(MemberId).ShouldFail().Is<ForbiddenError>();
    }

    [Test]
    public void Given_UserWithoutMemberLink_CanAccessMember_Should_FailWithForbidden()
    {
        new FakeCurrentUser().CanAccessMember(MemberId).ShouldFail().Is<ForbiddenError>();
    }
}
