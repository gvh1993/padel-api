using Padel.Application.Common;
using Padel.UnitTests.Fakes;

namespace Padel.UnitTests.Application.Common;

public class CurrentUserExtensionsTests
{
    private static readonly Guid MemberId = Guid.NewGuid();

    [Test]
    public async Task Given_Admin_EnsureCanAccessMember_Should_Allow()
    {
        await Assert.That(() => FakeCurrentUser.Admin().EnsureCanAccessMember(MemberId)).ThrowsNothing();
    }

    [Test]
    public async Task Given_SameMember_EnsureCanAccessMember_Should_Allow()
    {
        await Assert.That(() => FakeCurrentUser.ForMember(MemberId).EnsureCanAccessMember(MemberId)).ThrowsNothing();
    }

    [Test]
    public async Task Given_OtherMember_EnsureCanAccessMember_Should_ThrowForbiddenException()
    {
        await Assert.That(() => FakeCurrentUser.ForMember(Guid.NewGuid()).EnsureCanAccessMember(MemberId))
            .Throws<ForbiddenException>();
    }

    [Test]
    public async Task Given_UserWithoutMemberLink_EnsureCanAccessMember_Should_ThrowForbiddenException()
    {
        await Assert.That(() => new FakeCurrentUser().EnsureCanAccessMember(MemberId)).Throws<ForbiddenException>();
    }
}
