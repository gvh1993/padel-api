using AwesomeAssertions;
using Padel.Application.Features.Members;
using Padel.Domain.Members;
using Padel.UnitTests.Builders;
using Padel.UnitTests.Fakes;

namespace Padel.UnitTests.Application.Members;

public class MemberIdentityResolverTests
{
    private readonly HandlerFixture _fixture = new();

    private MemberIdentityResolver Resolver => new(_fixture.Members, _fixture.UnitOfWork);

    [Test]
    public async Task Resolve_WhenVerifiedEmailOfAnUnlinkedMember_ShouldLinkTheAccount()
    {
        var member = _fixture.Add(MemberBuilder.New().WithEmail("jan@example.com"));

        var memberId = await Resolver.ResolveAsync("account-1", "JAN@example.com");

        memberId.Should().Be(member.Id);
        member.IdentityId.Should().Be("account-1");
        _fixture.UnitOfWork.PublishedEvents.Should().ContainSingle().Which.Should().BeOfType<MemberIdentityLinked>();
    }

    [Test]
    public async Task Resolve_GivenLinkedAccount_ShouldFindTheMemberWithoutEmail()
    {
        var member = _fixture.Add(MemberBuilder.New().WithEmail("jan@example.com").LinkedTo("account-1"));

        var memberId = await Resolver.ResolveAsync("account-1", verifiedEmail: null);

        memberId.Should().Be(member.Id);
        _fixture.UnitOfWork.SaveCount.Should().Be(0);
    }

    [Test]
    public async Task Resolve_WhenNoVerifiedEmail_ShouldNotLink()
    {
        var member = _fixture.Add(MemberBuilder.New().WithEmail("jan@example.com"));

        var memberId = await Resolver.ResolveAsync("account-1", verifiedEmail: null);

        memberId.Should().BeNull();
        member.IdentityId.Should().BeNull();
    }

    [Test]
    public async Task Resolve_GivenEmailOfAMemberLinkedToAnotherAccount_ShouldNotTakeItOver()
    {
        var member = _fixture.Add(MemberBuilder.New().WithEmail("jan@example.com").LinkedTo("account-1"));

        var memberId = await Resolver.ResolveAsync("account-2", "jan@example.com");

        memberId.Should().BeNull();
        member.IdentityId.Should().Be("account-1");
    }

    [Test]
    public async Task Resolve_WhenUnknownEmail_ShouldReturnNull()
    {
        _fixture.Add(MemberBuilder.New().WithEmail("jan@example.com"));

        (await Resolver.ResolveAsync("account-1", "piet@example.com")).Should().BeNull();
    }
}
