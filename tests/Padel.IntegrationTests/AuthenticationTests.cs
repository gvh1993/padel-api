using System.Net;
using AwesomeAssertions;
using Padel.Api.Features.Members;
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

        var response = await As(member).Members.Register(new RegisterMemberRequest
        {
            FirstName = "Sneaky",
            LastName = "Member",
            Email = UniqueEmail(),
        });

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
}
