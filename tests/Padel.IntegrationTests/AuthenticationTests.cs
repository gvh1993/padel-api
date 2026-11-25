using System.Net;
using AwesomeAssertions;
using Padel.IntegrationTests.Builders;
using Padel.IntegrationTests.Infrastructure;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class AuthenticationTests(PadelApiFactory factory) : IntegrationTestBase(factory)
{
    [Test]
    public async Task Given_NoToken_ListCourts_Should_Return401()
    {
        var response = await Anonymous.Courts.List();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Given_TokenNotIssuedByKeycloak_ListCourts_Should_Return401()
    {
        var response = await WithToken(Tokens.Forged()).Courts.List();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Given_MemberToken_RegisterMember_Should_Return403()
    {
        var member = await RegisterMemberAsync();

        var response = await As(member).Members.Register(RegisterMemberRequestBuilder.New().Generate());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Given_MemberToken_ListMembers_Should_Return403()
    {
        var member = await RegisterMemberAsync();

        var response = await As(member).Members.List();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Given_TokenWithoutMemberLink_ListBookings_Should_Return403Problem()
    {
        var response = await Unlinked.Bookings.List();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.ReadProblemAsync();
        problem.TraceId.Should().NotBeNull();
    }

    [Test]
    public async Task Given_AccountWithTheMembersVerifiedEmail_GetMember_Should_LinkAndAllowAccess()
    {
        var member = await RegisterMemberAsync();

        var response = await As(member).Members.Get(member.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task Given_AccountWithTheMembersUnverifiedEmail_GetMember_Should_Return403()
    {
        var member = await RegisterMemberAsync();
        var unverified = WithToken(await Tokens.ForEmailAsync(member.Email, emailVerified: false));

        var response = await unverified.Members.Get(member.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Given_AccountWithAnotherEmail_GetMember_Should_Return403()
    {
        var member = await RegisterMemberAsync();
        var other = WithToken(await Tokens.ForEmailAsync(UniqueEmail("other")));

        var response = await other.Members.Get(member.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
