using System.Net;
using Padel.Api.Features.Members;
using Padel.Application.Features.Members;
using Padel.Domain.Members;
using Padel.IntegrationTests.Clients;
using Padel.IntegrationTests.Infrastructure;
using TUnit.Assertions.Enums;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class MembersApiTests(PadelApiFactory factory) : IntegrationTestBase(factory)
{
    [Test]
    public async Task RegisterMember_Should_Return201WithLocationAndNormalizedMember()
    {
        var email = UniqueEmail("Gert-Jan");

        var response = await Admin.Members.Register(new RegisterMemberCommand
        {
            FirstName = " Gert-Jan ",
            LastName = "De Vries",
            Email = email.ToUpperInvariant(),
            Phone = "+31 (0)10 123 4567",
        });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var member = response.Value();
        await Assert.That(member.FirstName).IsEqualTo("Gert-Jan");
        await Assert.That(member.Email).IsEqualTo(email.ToLowerInvariant());
        await Assert.That(member.Phone).IsEqualTo("310101234567");
        await Assert.That(member.Status).IsEqualTo(MemberStatus.Active);
        await Assert.That(response.Headers!.Location!.AbsolutePath).IsEqualTo($"/api/members/{member.Id}");
    }

    [Test]
    public async Task Given_ExistingEmail_RegisterMember_Should_Return409Problem()
    {
        var existing = await RegisterMemberAsync();

        var response = await Admin.Members.Register(new RegisterMemberCommand
        {
            FirstName = "Other",
            LastName = "Person",
            Email = existing.Email.ToUpperInvariant(),
        });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        var problem = await response.ReadProblemAsync();
        await Assert.That(problem.Detail).Contains("already exists");
    }

    [Test]
    public async Task Given_ConcurrentRegistrationsWithSameEmail_Should_CreateExactlyOneMember()
    {
        var email = UniqueEmail("race");

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(i => Admin.Members.Register(new RegisterMemberCommand
        {
            FirstName = $"Racer{i}",
            LastName = "Test",
            Email = email,
        })));

        var statusCodes = responses.Select(r => r.StatusCode).ToList();
        await Assert.That(statusCodes.Count(s => s == HttpStatusCode.Created)).IsEqualTo(1);
        await Assert.That(statusCodes.Count(s => s == HttpStatusCode.Conflict)).IsEqualTo(7);
    }

    [Test]
    public async Task Given_InvalidFields_RegisterMember_Should_Return400WithErrorsPerField()
    {
        var response = await Admin.Members.Register(new RegisterMemberCommand
        {
            FirstName = "",
            LastName = "Jansen",
            Email = "not-an-email",
            Phone = "12",
        });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        await Assert.That(problem.Errors.Keys).IsEquivalentTo(["firstName", "email", "phone"]);
    }

    [Test]
    public async Task Given_MemberToken_GetOwnProfile_Should_Return200()
    {
        var member = await RegisterMemberAsync();

        var response = await As(member).Members.Get(member.Id);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Value().Email).IsEqualTo(member.Email);
    }

    [Test]
    public async Task Given_MemberToken_GetOtherMember_Should_Return403()
    {
        var member = await RegisterMemberAsync();
        var other = await RegisterMemberAsync();

        var response = await As(member).Members.Get(other.Id);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Given_UnknownId_GetMember_Should_Return404Problem()
    {
        var response = await Admin.Members.Get(Guid.NewGuid());

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        var problem = await response.ReadProblemAsync();
        await Assert.That(problem.Status).IsEqualTo(404);
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

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var reloaded = (await Admin.Members.Get(member.Id)).Value();
        await Assert.That(reloaded.FirstName).IsEqualTo("Renamed");
        await Assert.That(reloaded.Email).IsEqualTo(newEmail);
        await Assert.That(reloaded.Phone).IsEqualTo("0612345678");
    }

    [Test]
    public async Task SuspendAndReactivate_Should_PersistStatusAndReason()
    {
        var member = await RegisterMemberAsync();

        var suspended = (await Admin.Members.Suspend(member.Id, new SuspendMemberRequest { Reason = "unpaid fees" })).Value();
        var reloaded = (await Admin.Members.Get(member.Id)).Value();
        var reactivated = (await Admin.Members.Reactivate(member.Id)).Value();

        await Assert.That(suspended.Status).IsEqualTo(MemberStatus.Suspended);
        await Assert.That(reloaded.SuspensionReason).IsEqualTo("unpaid fees");
        await Assert.That(reactivated.Status).IsEqualTo(MemberStatus.Active);
        await Assert.That(reactivated.SuspensionReason).IsNull();
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
        await Assert.That(page.TotalCount).IsEqualTo(3);
        await Assert.That(page.TotalPages).IsEqualTo(2);
        await Assert.That(page.Items.Select(m => m.FirstName)).IsEquivalentTo(["Cees", "Bram"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Given_StatusFilter_ListMembers_Should_ReturnOnlyThatStatus()
    {
        var tag = Guid.NewGuid().ToString("N")[..10];
        var suspended = await RegisterMemberAsync("Suspended", tag);
        await RegisterMemberAsync("Active", tag);
        await Admin.Members.Suspend(suspended.Id, new SuspendMemberRequest());

        var page = (await Admin.Members.List(new MemberListParameters { Search = tag, Status = MemberStatus.Suspended })).Value();

        await Assert.That(page.Items).HasSingleItem();
        await Assert.That(page.Items[0].Id).IsEqualTo(suspended.Id);
    }

    [Test]
    public async Task Given_UnknownSortFieldAndInvalidPageSize_ListMembers_Should_Return400()
    {
        var response = await Admin.Members.List(new MemberListParameters { Sort = "password", PageSize = 500 });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        await Assert.That(problem.Errors.Keys).IsEquivalentTo(["sort", "pageSize"]);
    }
}
