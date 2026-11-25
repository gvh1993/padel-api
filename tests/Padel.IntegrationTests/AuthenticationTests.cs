using System.Net;
using Padel.Application.Features.Members;
using Padel.IntegrationTests.Infrastructure;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class AuthenticationTests(PadelApiFactory factory) : IntegrationTestBase(factory)
{
    [Test]
    public async Task Given_NoToken_ListCourts_Should_Return401()
    {
        var response = await Anonymous.Courts.List();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Given_TokenNotIssuedByKeycloak_ListCourts_Should_Return401()
    {
        var response = await WithToken(Tokens.Forged()).Courts.List();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Given_MemberToken_RegisterMember_Should_Return403()
    {
        var member = await RegisterMemberAsync();

        var response = await As(member).Members.Register(new RegisterMemberCommand
        {
            FirstName = "Sneaky",
            LastName = "Member",
            Email = UniqueEmail(),
        });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Given_MemberToken_ListMembers_Should_Return403()
    {
        var member = await RegisterMemberAsync();

        var response = await As(member).Members.List();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Given_TokenWithoutMemberLink_ListBookings_Should_Return403Problem()
    {
        var response = await Unlinked.Bookings.List();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        var problem = await response.ReadProblemAsync();
        await Assert.That(problem.TraceId).IsNotNull();
    }
}
