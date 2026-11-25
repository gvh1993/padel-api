using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Padel.Infrastructure.Persistence;
using Padel.Infrastructure.Persistence.Outbox;
using Padel.IntegrationTests.Infrastructure;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class OutboxCleanupTests(PadelApiFactory factory)
{
    [Test]
    public async Task Cleaner_ShouldDeleteOnlyHandledMessagesOlderThanTheRetention()
    {
        var now = DateTimeOffset.UtcNow;
        var oldHandled = Message(processedOn: now.AddDays(-8));
        var recentHandled = Message(processedOn: now.AddDays(-1));
        var oldFailed = Message(processedOn: null, attempts: 5);

        await using (var context = CreateContext())
        {
            context.OutboxMessages.AddRange(oldHandled, recentHandled, oldFailed);
            await context.SaveChangesAsync();
        }

        // The cleaner runs every 200 ms in the tests.
        var ids = new[] { oldHandled.Id, recentHandled.Id, oldFailed.Id };
        List<Guid> remaining;
        var deadline = DateTime.UtcNow.AddSeconds(15);
        do
        {
            await Task.Delay(200);
            await using var context = CreateContext();
            remaining = await context.OutboxMessages.Where(m => ids.Contains(m.Id)).Select(m => m.Id).ToListAsync();
        }
        while (remaining.Contains(oldHandled.Id) && DateTime.UtcNow < deadline);

        remaining.Should().BeEquivalentTo(new[] { recentHandled.Id, oldFailed.Id });
    }

    private PadelDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<PadelDbContext>().UseNpgsql(factory.DatabaseConnectionString).Options);

    private static OutboxMessage Message(DateTimeOffset? processedOn, int attempts = 0) => new()
    {
        Id = Guid.CreateVersion7(),
        Type = "Padel.Domain.Test",
        Payload = "{}",
        OccurredOn = DateTimeOffset.UtcNow.AddDays(-10),
        ProcessedOn = processedOn,
        Attempts = attempts,
    };
}
