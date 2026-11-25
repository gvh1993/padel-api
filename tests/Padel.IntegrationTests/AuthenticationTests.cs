using System.Net;
using AwesomeAssertions;
using Padel.IntegrationTests.Builders;
using Padel.IntegrationTests.Infrastructure;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class AuthenticationTests(PadelApiFactory factory) : IntegrationTestBase(factory)
{
    [Test]
    public async Task ListCourts_GivenNoToken_ShouldReturn401()
    {
        var response = await Anonymous.Courts.List();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ListCourts_GivenTokenNotIssuedByKeycloak_ShouldReturn401()
    {
        var response = await WithToken(Tokens.Forged()).Courts.List();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task RegisterMember_GivenMemberToken_ShouldReturn403()
    {
        var member = await RegisterMemberAsync();

        var response = await As(member).Members.Register(RegisterMemberRequestBuilder.New().Generate());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task ListMembers_GivenMemberToken_ShouldReturn403()
    {
        var member = await RegisterMemberAsync();

        var response = await As(member).Members.List();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task ListBookings_GivenTokenWithoutMemberLink_ShouldReturn403Problem()
    {
        var response = await Unlinked.Bookings.List();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.ReadProblemAsync();
        problem.TraceId.Should().NotBeNull();
    }

    [Test]
    public async Task GetMember_GivenAccountWithTheMembersVerifiedEmail_ShouldLinkAndAllowAccess()
    {
        var member = await RegisterMemberAsync();

        var response = await As(member).Members.Get(member.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetMember_GivenAccountWithTheMembersUnverifiedEmail_ShouldReturn403()
    {
        var member = await RegisterMemberAsync();
        var unverified = WithToken(await Tokens.ForEmailAsync(member.Email, emailVerified: false));

        var response = await unverified.Members.Get(member.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetMember_GivenAccountWithAnotherEmail_ShouldReturn403()
    {
        var member = await RegisterMemberAsync();
        var other = WithToken(await Tokens.ForEmailAsync(UniqueEmail("other")));

        var response = await other.Members.Get(member.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
