using System.Net;
using AwesomeAssertions;
using Padel.Api.Features.Members;
using Padel.Domain.Members;
using Padel.IntegrationTests.Builders;
using Padel.IntegrationTests.Clients;
using Padel.IntegrationTests.Infrastructure;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class MembersApiTests(PadelApiFactory factory) : IntegrationTestBase(factory)
{
    [Test]
    public async Task RegisterMember_ShouldReturn201WithLocationAndNormalizedMember()
    {
        var email = UniqueEmail("Gert-Jan");

        var response = await Admin.Members.Register(RegisterMemberRequestBuilder.New()
            .WithName(" Gert-Jan ", "De Vries")
            .WithEmail(email.ToUpperInvariant())
            .WithPhone("+31 (0)10 123 4567")
            .Generate());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var member = response.Value();
        member.FirstName.Should().Be("Gert-Jan");
        member.Email.Should().Be(email.ToLowerInvariant());
        member.Phone.Should().Be("310101234567");
        member.Status.Should().Be(MemberStatus.Active);
        response.Headers!.Location!.AbsolutePath.Should().Be($"/api/v1/members/{member.Id}");
    }

    [Test]
    public async Task RegisterMember_GivenExistingEmail_ShouldReturn409Problem()
    {
        var existing = await RegisterMemberAsync();

        var response = await Admin.Members.Register(RegisterMemberRequestBuilder.New().WithEmail(existing.Email.ToUpperInvariant()).Generate());

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.ReadProblemAsync();
        problem.Detail.Should().Contain("already exists");
        problem.ErrorCode.Should().Be("Member.EmailInUse");
    }

    [Test]
    public async Task RegisterMember_WhenConcurrentRegistrationsWithSameEmail_ShouldCreateExactlyOneMember()
    {
        var email = UniqueEmail("race");

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            Admin.Members.Register(RegisterMemberRequestBuilder.New().WithEmail(email).Generate())));

        var statusCodes = responses.Select(r => r.StatusCode).ToList();
        statusCodes.Count(s => s == HttpStatusCode.Created).Should().Be(1);
        statusCodes.Count(s => s == HttpStatusCode.Conflict).Should().Be(7);
    }

    [Test]
    public async Task RegisterMember_WhenInvalidFields_ShouldReturn400WithErrorsPerField()
    {
        var response = await Admin.Members.Register(RegisterMemberRequestBuilder.New()
            .WithName("", "Jansen")
            .WithEmail("not-an-email")
            .WithPhone("12")
            .Generate());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Errors.Keys.Should().BeEquivalentTo(new[] { "firstName", "email", "phone" });
    }

    [Test]
    public async Task GetMember_GivenTokenOfTheMember_ShouldReturn200()
    {
        var member = await RegisterMemberAsync();

        var response = await As(member).Members.Get(member.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Value().Email.Should().Be(member.Email);
    }

    [Test]
    public async Task GetMember_GivenTokenOfAnotherMember_ShouldReturn403()
    {
        var member = await RegisterMemberAsync();
        var other = await RegisterMemberAsync();

        var response = await As(member).Members.Get(other.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetMember_WhenUnknownId_ShouldReturn404Problem()
    {
        var response = await Admin.Members.Get(Guid.NewGuid());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.ReadProblemAsync();
        problem.Status.Should().Be(404);
    }

    [Test]
    public async Task UpdateMemberProfile_GivenTokenOfTheMember_ShouldPersistChanges()
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
    public async Task SuspendAndReactivate_ShouldPersistStatusAndReason()
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
    public async Task ListMembers_WhenSearchSortAndCursor_ShouldReturnFilteredSortedPages()
    {
        var tag = Guid.NewGuid().ToString("N")[..10];
        await RegisterMemberAsync(RegisterMemberRequestBuilder.New().WithName("Anna", $"Visser{tag}"));
        await RegisterMemberAsync(RegisterMemberRequestBuilder.New().WithName("Bram", $"Bakker{tag}"));
        await RegisterMemberAsync(RegisterMemberRequestBuilder.New().WithName("Cees", $"Jansen{tag}"));
        var parameters = new MemberListParameters { Search = tag, Sort = "-firstName", PageSize = 2 };

        var first = (await Admin.Members.List(parameters)).Value();
        var second = (await Admin.Members.List(parameters with { Cursor = first.NextCursor })).Value();

        first.Items.Select(m => m.FirstName).Should().Equal(new[] { "Cees", "Bram" });
        first.NextCursor.Should().NotBeNull();
        second.Items.Select(m => m.FirstName).Should().Equal(new[] { "Anna" });
        second.NextCursor.Should().BeNull();
    }

    [Test]
    public async Task ListMembers_WhenPagingThroughTiesAndAnEnumSort_ShouldReturnEveryMemberOnceInOrder()
    {
        // Same last name and status for several members: the id breaks the ties, also in PostgreSQL.
        var tag = Guid.NewGuid().ToString("N")[..10];
        var members = new List<RegisterMemberResponse>();
        for (var i = 0; i < 5; i++)
        {
            members.Add(await RegisterMemberAsync(RegisterMemberRequestBuilder.New().WithName($"Member{i}", $"Same{tag}")));
        }

        await Admin.Members.Suspend(members[1].Id, new SuspendMemberRequest());
        await Admin.Members.Suspend(members[3].Id, new SuspendMemberRequest());

        var seen = new List<ListMembersItem>();
        var parameters = new MemberListParameters { Search = tag, Sort = "-status,lastName", PageSize = 2 };
        string? cursor = null;
        do
        {
            var page = (await Admin.Members.List(parameters with { Cursor = cursor })).Value();
            seen.AddRange(page.Items);
            cursor = page.NextCursor;
        }
        while (cursor is not null);

        seen.Select(m => m.Id).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(members.Select(m => m.Id));
        seen.Select(m => m.Status).Should().Equal(
            MemberStatus.Suspended, MemberStatus.Suspended, MemberStatus.Active, MemberStatus.Active, MemberStatus.Active);
    }

    [Test]
    public async Task ListMembers_WhenCursorOfAnotherSort_ShouldReturn400()
    {
        var tag = Guid.NewGuid().ToString("N")[..10];
        await RegisterMemberAsync(RegisterMemberRequestBuilder.New().WithName("Anna", tag));
        await RegisterMemberAsync(RegisterMemberRequestBuilder.New().WithName("Bram", tag));
        var first = (await Admin.Members.List(new MemberListParameters { Search = tag, Sort = "firstName", PageSize = 1 })).Value();

        var response = await Admin.Members.List(new MemberListParameters { Search = tag, Sort = "email", Cursor = first.NextCursor });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadProblemAsync()).Errors.Keys.Should().BeEquivalentTo(new[] { "cursor" });
    }

    [Test]
    public async Task ListMembers_WhenStatusFilter_ShouldReturnOnlyThatStatus()
    {
        var tag = Guid.NewGuid().ToString("N")[..10];
        var suspended = await RegisterMemberAsync(RegisterMemberRequestBuilder.New().WithName("Suspended", tag));
        await RegisterMemberAsync(RegisterMemberRequestBuilder.New().WithName("Active", tag));
        await Admin.Members.Suspend(suspended.Id, new SuspendMemberRequest());

        var page = (await Admin.Members.List(new MemberListParameters { Search = tag, Status = MemberStatus.Suspended })).Value();

        page.Items.Should().ContainSingle();
        page.Items[0].Id.Should().Be(suspended.Id);
    }

    [Test]
    public async Task ListMembers_WhenUnknownSortFieldAndInvalidPageSize_ShouldReturn400()
    {
        var response = await Admin.Members.List(new MemberListParameters { Sort = "password", PageSize = 500 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Errors.Keys.Should().BeEquivalentTo(new[] { "sort", "pageSize" });
    }
}
