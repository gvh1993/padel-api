using Padel.Application.Common;
using Padel.Domain.Common;
using Padel.UnitTests.Fakes;

namespace Padel.UnitTests.Application.Common;

public class CurrentUserExtensionsTests
{
    private static readonly Guid MemberId = Guid.NewGuid();

    [Test]
    public async Task Given_Admin_CanAccessMember_Should_Succeed()
    {
        await Assert.That(FakeCurrentUser.Admin().CanAccessMember(MemberId) is Success).IsTrue();
    }

    [Test]
    public async Task Given_SameMember_CanAccessMember_Should_Succeed()
    {
        await Assert.That(FakeCurrentUser.ForMember(MemberId).CanAccessMember(MemberId) is Success).IsTrue();
    }

    [Test]
    public async Task Given_OtherMember_CanAccessMember_Should_FailWithForbidden()
    {
        await FakeCurrentUser.ForMember(Guid.NewGuid()).CanAccessMember(MemberId).ShouldFail().Is<ForbiddenError>();
    }

    [Test]
    public async Task Given_UserWithoutMemberLink_CanAccessMember_Should_FailWithForbidden()
    {
        await new FakeCurrentUser().CanAccessMember(MemberId).ShouldFail().Is<ForbiddenError>();
    }
}
