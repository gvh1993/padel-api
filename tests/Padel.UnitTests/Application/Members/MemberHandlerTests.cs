using AwesomeAssertions;
using Padel.Application.Common;
using Padel.Application.Features.Members;
using Padel.Domain.Common;
using Padel.Domain.Members;
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
    public async Task RegisterMember_Should_PersistMemberAndRaiseMemberRegistered()
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
    public async Task Given_EmailAlreadyInUse_RegisterMember_Should_FailWithConflict()
    {
        _fixture.AddMember("jan@example.com");

        var error = await RegisterHandler.Handle(
            new RegisterMemberCommand { FirstName = "J", LastName = "J", Email = "JAN@example.com" },
            CancellationToken.None).ShouldFail().Is<ConflictError>();

        error.Description.Should().Contain("already exists");
        _fixture.Members.Store.Should().ContainSingle();
        _fixture.UnitOfWork.SaveCount.Should().Be(0);
    }

    [Test]
    public async Task UpdateMemberProfile_Should_ReplaceProfileAndSave()
    {
        var member = _fixture.AddMember();

        var result = await UpdateHandler.Handle(
            new UpdateMemberProfileCommand { Id = member.Id, FirstName = "Jan", LastName = "Jansen", Email = "jan@example.com" },
            CancellationToken.None).ShouldSucceed();

        result.FirstName.Should().Be("Jan");
        result.Email.Should().Be("jan@example.com");
        _fixture.UnitOfWork.SaveCount.Should().Be(1);
    }

    [Test]
    public async Task Given_UnchangedEmail_UpdateMemberProfile_Should_NotReportConflict()
    {
        var member = _fixture.AddMember("gj@example.com");

        var result = await UpdateHandler.Handle(
            new UpdateMemberProfileCommand { Id = member.Id, FirstName = "Gert", LastName = "De Vries", Email = "gj@example.com" },
            CancellationToken.None).ShouldSucceed();

        result.FirstName.Should().Be("Gert");
    }

    [Test]
    public async Task Given_EmailOfAnotherMember_UpdateMemberProfile_Should_FailWithConflict()
    {
        var member = _fixture.AddMember("gj@example.com");
        _fixture.AddMember("jan@example.com");

        await UpdateHandler.Handle(
            new UpdateMemberProfileCommand { Id = member.Id, FirstName = "Gert", LastName = "De Vries", Email = "jan@example.com" },
            CancellationToken.None).ShouldFail().Is<ConflictError>();
    }

    [Test]
    public async Task Given_CallerIsAnotherMember_UpdateMemberProfile_Should_FailWithForbidden()
    {
        var member = _fixture.AddMember();
        _fixture.CurrentUser = FakeCurrentUser.ForMember(Guid.NewGuid());

        await UpdateHandler.Handle(
            new UpdateMemberProfileCommand { Id = member.Id, FirstName = "Jan", LastName = "Jansen", Email = "jan@example.com" },
            CancellationToken.None).ShouldFail().Is<ForbiddenError>();
    }

    [Test]
    public async Task SuspendMember_Should_SuspendWithReasonAndRaiseMemberSuspended()
    {
        var member = _fixture.AddMember();

        var result = await SuspendHandler.Handle(new SuspendMemberCommand(member.Id, "misbehaving"), CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(MemberStatus.Suspended);
        result.SuspensionReason.Should().Be("misbehaving");
        _fixture.UnitOfWork.PublishedEvents.Single().Should().BeOfType<MemberSuspended>();
    }

    [Test]
    public async Task Given_UnknownMember_SuspendMember_Should_FailWithNotFound()
    {
        await SuspendHandler.Handle(new SuspendMemberCommand(Guid.NewGuid(), null), CancellationToken.None).ShouldFail().Is<NotFoundError>();

        _fixture.UnitOfWork.SaveCount.Should().Be(0);
    }

    [Test]
    public async Task Given_SuspendedMember_ReactivateMember_Should_ReactivateAndRaiseMemberReactivated()
    {
        var member = _fixture.AddMember();
        member.Suspend();
        member.ClearDomainEvents();

        var result = await ReactivateHandler.Handle(new ReactivateMemberCommand(member.Id), CancellationToken.None).ShouldSucceed();

        result.Status.Should().Be(MemberStatus.Active);
        _fixture.UnitOfWork.PublishedEvents.Single().Should().BeOfType<MemberReactivated>();
    }

    [Test]
    public async Task Given_CallerIsTheMember_GetMember_Should_ReturnMember()
    {
        var member = _fixture.AddMember();
        _fixture.CurrentUser = FakeCurrentUser.ForMember(member.Id);

        var result = await GetHandler.Handle(new GetMemberQuery(member.Id), CancellationToken.None).ShouldSucceed();

        result.LastName.Should().Be("De Vries");
    }

    [Test]
    public async Task Given_CallerIsAnotherMember_GetMember_Should_FailWithForbidden()
    {
        var member = _fixture.AddMember();
        _fixture.CurrentUser = FakeCurrentUser.ForMember(Guid.NewGuid());

        await GetHandler.Handle(new GetMemberQuery(member.Id), CancellationToken.None).ShouldFail().Is<ForbiddenError>();
    }

    [Test]
    public async Task Given_UnknownMember_GetMember_Should_FailWithNotFound()
    {
        await GetHandler.Handle(new GetMemberQuery(Guid.NewGuid()), CancellationToken.None).ShouldFail().Is<NotFoundError>();
    }

    [Test]
    public async Task ListMembers_Should_SortByLastNameThenFirstNameByDefault()
    {
        _fixture.AddMember("c@example.com", "Cees", "Bakker");
        _fixture.AddMember("a@example.com", "Anna", "Visser");
        _fixture.AddMember("b@example.com", "Bram", "Bakker");

        var result = await ListHandler.Handle(new ListMembersQuery(), CancellationToken.None).ShouldSucceed();

        result.Items.Select(m => m.FirstName).Should().Equal(new[] { "Bram", "Cees", "Anna" });
    }

    [Test]
    public async Task Given_DescendingSort_ListMembers_Should_ReverseOrder()
    {
        _fixture.AddMember("a@example.com", "Anna", "Visser");
        _fixture.AddMember("b@example.com", "Bram", "Bakker");

        var result = await ListHandler.Handle(new ListMembersQuery { Sort = "-email" }, CancellationToken.None).ShouldSucceed();

        result.Items.Select(m => m.Email).Should().Equal(new[] { "b@example.com", "a@example.com" });
    }

    [Test]
    public async Task Given_StatusAndSearchFilters_ListMembers_Should_ReturnOnlyMatches()
    {
        var suspended = _fixture.AddMember("anna@example.com", "Anna", "Visser");
        suspended.Suspend();
        _fixture.AddMember("annabel@example.com", "Annabel", "Bakker");
        _fixture.AddMember("bram@example.com", "Bram", "Visser");

        var result = await ListHandler.Handle(
            new ListMembersQuery { Status = MemberStatus.Active, Search = "ANNA" },
            CancellationToken.None).ShouldSucceed();

        result.Items.Should().ContainSingle();
        result.Items[0].FirstName.Should().Be("Annabel");
    }

    [Test]
    public async Task Given_SecondPage_ListMembers_Should_ReturnRemainingItemsAndTotals()
    {
        for (var i = 0; i < 5; i++)
        {
            _fixture.AddMember($"m{i}@example.com", $"Member{i}", "Test");
        }

        var result = await ListHandler.Handle(new ListMembersQuery { Page = 2, PageSize = 3, Sort = "firstName" }, CancellationToken.None).ShouldSucceed();

        result.Items.Select(m => m.FirstName).Should().Equal(new[] { "Member3", "Member4" });
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(2);
        result.Page.Should().Be(2);
    }
}
