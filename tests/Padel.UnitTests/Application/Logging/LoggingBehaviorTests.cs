using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using Padel.Application.Common.Behaviors;
using Padel.Application.Common.Messaging;
using Padel.Application.Common.Querying;
using Padel.Application.Features.Members;
using Padel.Domain.Common;
using Padel.UnitTests.Fakes;

namespace Padel.UnitTests.Application.Logging;

public sealed class LoggingBehaviorTests : IDisposable
{
    private static readonly Guid MemberId = Guid.CreateVersion7();

    private static readonly RegisterMemberCommand Registration = new()
    {
        FirstName = "Jan",
        LastName = "Jansen",
        Email = "jan@example.com",
        Phone = "0612345678",
    };

    private readonly FakeLoggerProvider _logs = new();
    private readonly LoggerFactory _loggerFactory;
    private readonly FakeCurrentUser _user = new() { UserId = "account-1", MemberId = MemberId };

    public LoggingBehaviorTests() => _loggerFactory = new LoggerFactory([_logs]);

    [Test]
    public async Task Handle_WhenUseCaseSucceeds_ShouldLogWhoWhatWentInAndWhatCameOut()
    {
        var dto = new MemberDto { Id = MemberId, FirstName = "Jan", LastName = "Jansen", Email = "jan@example.com" };

        await Behavior<RegisterMemberCommand, MemberDto>().Handle(Registration, Next<MemberDto>(dto), CancellationToken.None);

        var entry = _logs.Collector.LatestRecord;
        entry.Category.Should().Be(UseCaseLog.Category);
        entry.Level.Should().Be(LogLevel.Information);
        entry.GetStructuredStateValue("UseCase").Should().Be("RegisterMemberCommand");
        entry.GetStructuredStateValue("UserId").Should().Be("account-1");
        entry.GetStructuredStateValue("MemberId").Should().Be(MemberId.ToString());
        entry.GetStructuredStateValue("Request").Should().Contain("\"email\":\"jan@example.com\"").And.Contain("\"phone\":\"0612345678\"");
        entry.GetStructuredStateValue("Result").Should().Contain(MemberId.ToString()).And.Contain("\"lastName\":\"Jansen\"");
    }

    [Test]
    public async Task Handle_WhenUseCaseFails_ShouldLogTheErrorCode()
    {
        var conflict = Error.Conflict("Member.EmailInUse", "A member with this email already exists.");

        await Behavior<RegisterMemberCommand, MemberDto>().Handle(Registration, Next<MemberDto>(conflict), CancellationToken.None);

        var entry = _logs.Collector.LatestRecord;
        entry.Level.Should().Be(LogLevel.Information);
        entry.GetStructuredStateValue("ErrorCode").Should().Be("Member.EmailInUse");
        entry.GetStructuredStateValue("ErrorDescription").Should().Be("A member with this email already exists.");
    }

    [Test]
    public async Task Handle_WhenUseCaseThrows_ShouldLogAnErrorAndRethrow()
    {
        var next = Substitute.For<RequestHandlerDelegate<MemberDto>>();
        next().Returns<Task<Result<MemberDto>>>(_ => throw new InvalidOperationException("boom"));

        await FluentActions.Awaiting(() => Behavior<RegisterMemberCommand, MemberDto>().Handle(Registration, next, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        var entry = _logs.Collector.LatestRecord;
        entry.Level.Should().Be(LogLevel.Error);
        entry.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Test]
    public async Task Handle_WhenResultIsAPage_ShouldLogCountIdsAndWhetherThereIsMore()
    {
        var members = new[] { new MemberDto { Id = Guid.CreateVersion7() }, new MemberDto { Id = Guid.CreateVersion7() } };
        var page = PagedResult<MemberDto>.Create(members, "next");

        await Behavior<ListMembersQuery, PagedResult<MemberDto>>().Handle(new ListMembersQuery { Search = "jan" }, Next<PagedResult<MemberDto>>(page), CancellationToken.None);

        var entry = _logs.Collector.LatestRecord;
        entry.GetStructuredStateValue("Request").Should().Contain("\"search\":\"jan\"");
        entry.GetStructuredStateValue("Result").Should().Be(
            $$"""{"count":2,"ids":["{{members[0].Id}}","{{members[1].Id}}"],"hasNextPage":true}""");
    }

    [Test]
    public async Task Handle_GivenAnonymousCaller_ShouldLogAnonymous()
    {
        _user.UserId = null;

        await Behavior<RegisterMemberCommand, MemberDto>().Handle(Registration, Next<MemberDto>(new MemberDto()), CancellationToken.None);

        _logs.Collector.LatestRecord.GetStructuredStateValue("UserId").Should().Be("anonymous");
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
        _logs.Dispose();
    }

    private LoggingBehavior<TRequest, TValue> Behavior<TRequest, TValue>()
        where TRequest : notnull
        where TValue : notnull =>
        new(_loggerFactory, _user);

    private static RequestHandlerDelegate<TValue> Next<TValue>(Result<TValue> result)
        where TValue : notnull =>
        () => Task.FromResult(result);
}
