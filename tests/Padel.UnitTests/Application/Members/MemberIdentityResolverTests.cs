using AwesomeAssertions;
using Padel.Application.Features.Members;
using Padel.Domain.Members;
using Padel.UnitTests.Fakes;

namespace Padel.UnitTests.Application.Members;

public class MemberIdentityResolverTests
{
    private readonly HandlerFixture _fixture = new();

    private MemberIdentityResolver Resolver => new(_fixture.Members, _fixture.UnitOfWork);

    [Test]
    public async Task Given_VerifiedEmailOfAnUnlinkedMember_Resolve_Should_LinkTheAccount()
    {
        var member = _fixture.AddMember("jan@example.com");

        var memberId = await Resolver.ResolveAsync("account-1", "JAN@example.com");

        memberId.Should().Be(member.Id);
        member.IdentityId.Should().Be("account-1");
        _fixture.UnitOfWork.PublishedEvents.Should().ContainSingle().Which.Should().BeOfType<MemberIdentityLinked>();
    }

    [Test]
    public async Task Given_LinkedAccount_Resolve_Should_FindTheMemberWithoutEmail()
    {
        var member = _fixture.AddMember("jan@example.com");
        member.LinkIdentity("account-1").ShouldSucceed();

        var memberId = await Resolver.ResolveAsync("account-1", verifiedEmail: null);

        memberId.Should().Be(member.Id);
        _fixture.UnitOfWork.SaveCount.Should().Be(0);
    }

    [Test]
    public async Task Given_NoVerifiedEmail_Resolve_Should_NotLink()
    {
        var member = _fixture.AddMember("jan@example.com");

        var memberId = await Resolver.ResolveAsync("account-1", verifiedEmail: null);

        memberId.Should().BeNull();
        member.IdentityId.Should().BeNull();
    }

    [Test]
    public async Task Given_EmailOfAMemberLinkedToAnotherAccount_Resolve_Should_NotTakeItOver()
    {
        var member = _fixture.AddMember("jan@example.com");
        member.LinkIdentity("account-1").ShouldSucceed();

        var memberId = await Resolver.ResolveAsync("account-2", "jan@example.com");

        memberId.Should().BeNull();
        member.IdentityId.Should().Be("account-1");
    }

    [Test]
    public async Task Given_UnknownEmail_Resolve_Should_ReturnNull()
    {
        _fixture.AddMember("jan@example.com");

        (await Resolver.ResolveAsync("account-1", "piet@example.com")).Should().BeNull();
    }
}
