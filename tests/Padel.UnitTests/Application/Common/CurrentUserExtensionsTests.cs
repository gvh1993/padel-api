using Padel.Application.Common;
using Padel.Domain.Common;
using Padel.UnitTests.Fakes;

namespace Padel.UnitTests.Application.Common;

public class CurrentUserExtensionsTests
{
    private static readonly Guid MemberId = Guid.NewGuid();

    [Test]
    public void CanAccessMember_GivenAdmin_ShouldSucceed()
    {
        FakeCurrentUser.Admin().CanAccessMember(MemberId).ShouldSucceed();
    }

    [Test]
    public void CanAccessMember_GivenSameMember_ShouldSucceed()
    {
        FakeCurrentUser.ForMember(MemberId).CanAccessMember(MemberId).ShouldSucceed();
    }

    [Test]
    public void CanAccessMember_GivenOtherMember_ShouldFailWithForbidden()
    {
        FakeCurrentUser.ForMember(Guid.NewGuid()).CanAccessMember(MemberId).ShouldFail().Is<ForbiddenError>();
    }

    [Test]
    public void CanAccessMember_GivenUserWithoutMemberLink_ShouldFailWithForbidden()
    {
        new FakeCurrentUser().CanAccessMember(MemberId).ShouldFail().Is<ForbiddenError>();
    }
}
