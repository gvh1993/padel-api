using AwesomeAssertions;
using Padel.Application.Features.Members;
using Padel.Domain.Common;
using Padel.Domain.Members;
using Padel.UnitTests.Builders;
using Padel.UnitTests.Fakes;

namespace Padel.UnitTests.Application.Members;

public class MemberHandlerTests
{
    private readonly HandlerFixture _fixture = new();

    private RegisterMemberCommandHandler RegisterHandler => new(_fixture.Members, _fixture.UnitOfWork, _fixture.Clock);
    private UpdateMemberProfileCommandHandler UpdateHandler => new(_fixture.Members, _fixture.UnitOfWork, _fixture.CurrentUser);
    private SuspendMemberCommandHandler SuspendHandler => new(_fixture.Members, _fixture.UnitOfWork);
    private ReactivateMemberCommandHandler ReactivateHandler => new(_fixture.Members, _fixture.UnitOfWork);
    private GetMemberQueryHandler GetHandler => new(_fixture.Members, _fixture.CurrentUser);
    private ListMembersQueryHandler ListHandler => new(_fixture.Members);

    [Test]
    public async Task RegisterMember_ShouldPersistMemberAndRaiseMemberRegistered()
    {
        var result = await RegisterHandler.Handle(
            new RegisterMemberCommand { FirstName = "Gert-Jan", LastName = "De Vries", Email = "GJ@Example.com", Phone = "010 123 4567" },
            CancellationToken.None).ShouldSucceed();

        var member = _fixture.Members.Store.Single();
        result.Id.Should().Be(member.Id);
        result.Email.Should().Be("gj@example.com");
        result.Phone.Should().Be("0101234567");
        result.RegisteredOn.Should().Be(HandlerFixture.Now);
        _fixture.UnitOfWork.SaveCount.Should().Be(1);
        _fixture.UnitOfWork.PublishedEvents.Single().Should().BeOfType<MemberRegistered>();
    }

    [Test]
    public async Task RegisterMember_GivenEmailAlreadyInUse_ShouldFailWithConflict()
    {
        _fixture.Add(MemberBuilder.New().WithEmail("jan@example.com"));

        var error = await RegisterHandler.Handle(
            new RegisterMemberCommand { FirstName = "J", LastName = "J", Email = "JAN@example.com" },
            CancellationToken.None).ShouldFail().Is<ConflictError>();

        error.Description.Should().Contain("already exists");
        _fixture.Members.Store.Should().ContainSingle();
        _fixture.UnitOfWork.SaveCount.Should().Be(0);
    }

    [Test]
    public async Task UpdateMemberProfile_ShouldReplaceProfileAndSave()
    {
        var member = _fixture.Add(MemberBuilder.New());

        var result = await UpdateHandler.Handle(
            new UpdateMemberProfileCommand { Id = member.Id, FirstName = "Jan", LastName = "Jansen", Email = "jan@example.com" },
            CancellationToken.None).ShouldSucceed();

        result.FirstName.Should().Be("Jan");
        result.Email.Should().Be("jan@example.com");
        _fixture.UnitOfWork.SaveCount.Should().Be(1);
    }

    [Test]
    public async Task UpdateMemberProfile_WhenUnchangedEmail_ShouldNotReportConflict()
    {
        var member = _fixture.Add(MemberBuilder.New().WithEmail("gj@example.com"));

        var result = await UpdateHandler.Handle(
            new UpdateMemberProfileCommand { Id = member.Id, FirstName = "Gert", LastName = "De Vries", Email = "gj@example.com" },
            CancellationToken.None).ShouldSucceed();

        result.FirstName.Should().Be("Gert");
    }

    [Test]
    public async Task UpdateMemberProfile_WhenEmailOfAnotherMember_ShouldFailWithConflict()
    {
        var member = _fixture.Add(MemberBuilder.New().WithEmail("gj@example.com"));
        _fixture.Add(MemberBuilder.New().WithEmail("jan@example.com"));

        await UpdateHandler.Handle(
            new UpdateMemberProfileCommand { Id = member.Id, FirstName = "Gert", LastName = "De Vries", Email = "jan@example.com" },
            CancellationToken.None).ShouldFail().Is<ConflictError>();
    }

    [Test]
    public async Task UpdateMemberProfile_GivenCallerIsAnotherMember_ShouldFailWithForbidden()
    {
        var member = _fixture.Add(MemberBuilder.New());
        _fixture.CurrentUser = FakeCurrentUser.ForMember(Guid.NewGuid());

        await UpdateHandler.Handle(
            new UpdateMemberProfileCommand { Id = member.Id, FirstName = "Jan", LastName = "Jansen", Email = "jan@example.com" },
            CancellationToken.None).ShouldFail().Is<ForbiddenError>();
    }

    [Test]
    public async Task SuspendMember_ShouldSuspendWithReasonAndRaiseMemberSuspended()
    {
        var member = _fixture.Add(MemberBuilder.New());

        var result = await SuspendHandler.Handle(new SuspendMemberCommand(member.Id, "misbehaving"), CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(MemberStatus.Suspended);
        result.SuspensionReason.Should().Be("misbehaving");
        _fixture.UnitOfWork.PublishedEvents.Single().Should().BeOfType<MemberSuspended>();
    }

    [Test]
    public async Task SuspendMember_WhenUnknownMember_ShouldFailWithNotFound()
    {
        await SuspendHandler.Handle(new SuspendMemberCommand(Guid.NewGuid(), null), CancellationToken.None).ShouldFail().Is<NotFoundError>();

        _fixture.UnitOfWork.SaveCount.Should().Be(0);
    }

    [Test]
    public async Task ReactivateMember_GivenSuspendedMember_ShouldReactivateAndRaiseMemberReactivated()
    {
        var member = _fixture.Add(MemberBuilder.New().Suspended());

        var result = await ReactivateHandler.Handle(new ReactivateMemberCommand(member.Id), CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(MemberStatus.Active);
        _fixture.UnitOfWork.PublishedEvents.Single().Should().BeOfType<MemberReactivated>();
    }

    [Test]
    public async Task GetMember_GivenCallerIsTheMember_ShouldReturnMember()
    {
        var member = _fixture.Add(MemberBuilder.New());
        _fixture.CurrentUser = FakeCurrentUser.ForMember(member.Id);

        var result = await GetHandler.Handle(new GetMemberQuery(member.Id), CancellationToken.None).ShouldSucceed();

        result.LastName.Should().Be(member.Name.Last);
    }

    [Test]
    public async Task GetMember_GivenCallerIsAnotherMember_ShouldFailWithForbidden()
    {
        var member = _fixture.Add(MemberBuilder.New());
        _fixture.CurrentUser = FakeCurrentUser.ForMember(Guid.NewGuid());

        await GetHandler.Handle(new GetMemberQuery(member.Id), CancellationToken.None).ShouldFail().Is<ForbiddenError>();
    }

    [Test]
    public async Task GetMember_WhenUnknownMember_ShouldFailWithNotFound()
    {
        await GetHandler.Handle(new GetMemberQuery(Guid.NewGuid()), CancellationToken.None).ShouldFail().Is<NotFoundError>();
    }

    [Test]
    public async Task ListMembers_ShouldSortByLastNameThenFirstNameByDefault()
    {
        _fixture.Add(MemberBuilder.New().WithEmail("c@example.com").WithName("Cees", "Bakker"));
        _fixture.Add(MemberBuilder.New().WithEmail("a@example.com").WithName("Anna", "Visser"));
        _fixture.Add(MemberBuilder.New().WithEmail("b@example.com").WithName("Bram", "Bakker"));

        var result = await ListHandler.Handle(new ListMembersQuery(), CancellationToken.None).ShouldSucceed();

        result.Items.Select(m => m.FirstName).Should().Equal(new[] { "Bram", "Cees", "Anna" });
    }

    [Test]
    public async Task ListMembers_WhenDescendingSort_ShouldReverseOrder()
    {
        _fixture.Add(MemberBuilder.New().WithEmail("a@example.com").WithName("Anna", "Visser"));
        _fixture.Add(MemberBuilder.New().WithEmail("b@example.com").WithName("Bram", "Bakker"));

        var result = await ListHandler.Handle(new ListMembersQuery { Sort = "-email" }, CancellationToken.None).ShouldSucceed();

        result.Items.Select(m => m.Email).Should().Equal(new[] { "b@example.com", "a@example.com" });
    }

    [Test]
    public async Task ListMembers_WhenStatusAndSearchFilters_ShouldReturnOnlyMatches()
    {
        var suspended = _fixture.Add(MemberBuilder.New().WithEmail("anna@example.com").WithName("Anna", "Visser").Suspended());
        _fixture.Add(MemberBuilder.New().WithEmail("annabel@example.com").WithName("Annabel", "Bakker"));
        _fixture.Add(MemberBuilder.New().WithEmail("bram@example.com").WithName("Bram", "Visser"));

        var result = await ListHandler.Handle(
            new ListMembersQuery { Status = MemberStatus.Active, Search = "ANNA" },
            CancellationToken.None).ShouldSucceed();

        result.Items.Should().ContainSingle();
        result.Items[0].FirstName.Should().Be("Annabel");
    }

    [Test]
    public async Task ListMembers_WhenSecondPage_ShouldReturnRemainingItemsAndTotals()
    {
        for (var i = 0; i < 5; i++)
        {
            _fixture.Add(MemberBuilder.New().WithEmail($"m{i}@example.com").WithName($"Member{i}", "Test"));
        }

        var result = await ListHandler.Handle(new ListMembersQuery { Page = 2, PageSize = 3, Sort = "firstName" }, CancellationToken.None).ShouldSucceed();

        result.Items.Select(m => m.FirstName).Should().Equal(new[] { "Member3", "Member4" });
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(2);
        result.Page.Should().Be(2);
    }
}
