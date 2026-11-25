using System.Net;
using AwesomeAssertions;
using Padel.Api.Features.Members;
using Padel.Domain.Members;
using Padel.IntegrationTests.Clients;
using Padel.IntegrationTests.Infrastructure;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class MembersApiTests(PadelApiFactory factory) : IntegrationTestBase(factory)
{
    [Test]
    public async Task RegisterMember_Should_Return201WithLocationAndNormalizedMember()
    {
        var email = UniqueEmail("Gert-Jan");

        var response = await Admin.Members.Register(new RegisterMemberRequest
        {
            FirstName = " Gert-Jan ",
            LastName = "De Vries",
            Email = email.ToUpperInvariant(),
            Phone = "+31 (0)10 123 4567",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var member = response.Value();
        member.FirstName.Should().Be("Gert-Jan");
        member.Email.Should().Be(email.ToLowerInvariant());
        member.Phone.Should().Be("310101234567");
        member.Status.Should().Be(MemberStatus.Active);
        response.Headers!.Location!.AbsolutePath.Should().Be($"/api/members/{member.Id}");
    }

    [Test]
    public async Task Given_ExistingEmail_RegisterMember_Should_Return409Problem()
    {
        var existing = await RegisterMemberAsync();

        var response = await Admin.Members.Register(new RegisterMemberRequest
        {
            FirstName = "Other",
            LastName = "Person",
            Email = existing.Email.ToUpperInvariant(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.ReadProblemAsync();
        problem.Detail.Should().Contain("already exists");
        problem.ErrorCode.Should().Be("Member.EmailInUse");
    }

    [Test]
    public async Task Given_ConcurrentRegistrationsWithSameEmail_Should_CreateExactlyOneMember()
    {
        var email = UniqueEmail("race");

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(i => Admin.Members.Register(new RegisterMemberRequest
        {
            FirstName = $"Racer{i}",
            LastName = "Test",
            Email = email,
        })));

        var statusCodes = responses.Select(r => r.StatusCode).ToList();
        statusCodes.Count(s => s == HttpStatusCode.Created).Should().Be(1);
        statusCodes.Count(s => s == HttpStatusCode.Conflict).Should().Be(7);
    }

    [Test]
    public async Task Given_InvalidFields_RegisterMember_Should_Return400WithErrorsPerField()
    {
        var response = await Admin.Members.Register(new RegisterMemberRequest
        {
            FirstName = "",
            LastName = "Jansen",
            Email = "not-an-email",
            Phone = "12",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Errors.Keys.Should().BeEquivalentTo(new[] { "firstName", "email", "phone" });
    }

    [Test]
    public async Task Given_MemberToken_GetOwnProfile_Should_Return200()
    {
        var member = await RegisterMemberAsync();

        var response = await As(member).Members.Get(member.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Value().Email.Should().Be(member.Email);
    }

    [Test]
    public async Task Given_MemberToken_GetOtherMember_Should_Return403()
    {
        var member = await RegisterMemberAsync();
        var other = await RegisterMemberAsync();

        var response = await As(member).Members.Get(other.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Given_UnknownId_GetMember_Should_Return404Problem()
    {
        var response = await Admin.Members.Get(Guid.NewGuid());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.ReadProblemAsync();
        problem.Status.Should().Be(404);
    }

    [Test]
    public async Task Given_MemberToken_UpdateOwnProfile_Should_PersistChanges()
    {
        var member = await RegisterMemberAsync();
        var newEmail = UniqueEmail("renamed");

        var response = await As(member).Members.UpdateProfile(member.Id, new UpdateMemberProfileRequest
        {
            FirstName = "Renamed",
            LastName = "Member",
            Email = newEmail,
            Phone = "0612345678",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reloaded = (await Admin.Members.Get(member.Id)).Value();
        reloaded.FirstName.Should().Be("Renamed");
        reloaded.Email.Should().Be(newEmail);
        reloaded.Phone.Should().Be("0612345678");
    }

    [Test]
    public async Task SuspendAndReactivate_Should_PersistStatusAndReason()
    {
        var member = await RegisterMemberAsync();

        var suspended = (await Admin.Members.Suspend(member.Id, new SuspendMemberRequest { Reason = "unpaid fees" })).Value();
        var reloaded = (await Admin.Members.Get(member.Id)).Value();
        var reactivated = (await Admin.Members.Reactivate(member.Id)).Value();

        suspended.Status.Should().Be(MemberStatus.Suspended);
        reloaded.SuspensionReason.Should().Be("unpaid fees");
        reactivated.Status.Should().Be(MemberStatus.Active);
        reactivated.SuspensionReason.Should().BeNull();
    }

    [Test]
    public async Task Given_SearchSortAndPaging_ListMembers_Should_ReturnFilteredSortedPage()
    {
        var tag = Guid.NewGuid().ToString("N")[..10];
        await RegisterMemberAsync("Anna", $"Visser{tag}");
        await RegisterMemberAsync("Bram", $"Bakker{tag}");
        await RegisterMemberAsync("Cees", $"Jansen{tag}");

        var response = await Admin.Members.List(new MemberListParameters
        {
            Search = tag,
            Sort = "-firstName",
            Page = 1,
            PageSize = 2,
        });

        var page = response.Value();
        page.TotalCount.Should().Be(3);
        page.TotalPages.Should().Be(2);
        page.Items.Select(m => m.FirstName).Should().Equal(new[] { "Cees", "Bram" });
    }

    [Test]
    public async Task Given_StatusFilter_ListMembers_Should_ReturnOnlyThatStatus()
    {
        var tag = Guid.NewGuid().ToString("N")[..10];
        var suspended = await RegisterMemberAsync("Suspended", tag);
        await RegisterMemberAsync("Active", tag);
        await Admin.Members.Suspend(suspended.Id, new SuspendMemberRequest());

        var page = (await Admin.Members.List(new MemberListParameters { Search = tag, Status = MemberStatus.Suspended })).Value();

        page.Items.Should().ContainSingle();
        page.Items[0].Id.Should().Be(suspended.Id);
    }

    [Test]
    public async Task Given_UnknownSortFieldAndInvalidPageSize_ListMembers_Should_Return400()
    {
        var response = await Admin.Members.List(new MemberListParameters { Sort = "password", PageSize = 500 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Errors.Keys.Should().BeEquivalentTo(new[] { "sort", "pageSize" });
    }
}
