using Padel.Application.Features.Bookings;
using Padel.Application.Features.Courts;
using Padel.Application.Features.Members;

namespace Padel.IntegrationTests.Infrastructure;

/// <summary>
/// Callers and test data helpers. Every test creates its own members and courts (unique emails and
/// names), so tests can share the database and run in parallel.
/// </summary>
public abstract class IntegrationTestBase(PadelApiFactory factory)
{
    protected PadelApi Admin { get; } = factory.CreateApi(() => factory.Tokens.AdminAsync());

    protected PadelApi Anonymous { get; } = factory.CreateApi();

    /// <summary>A Keycloak user linked to <paramref name="member"/> through the <c>member_id</c> attribute.</summary>
    protected PadelApi As(MemberDto member) => factory.CreateApi(() => factory.Tokens.ForMemberAsync(member.Id));

    /// <summary>A Keycloak user that is not linked to a member.</summary>
    protected PadelApi Unlinked { get; } = factory.CreateApi(() => factory.Tokens.WithoutMemberLinkAsync());

    protected PadelApi WithToken(string token) => factory.CreateApi(() => Task.FromResult(token));

    protected KeycloakTokens Tokens => factory.Tokens;

    protected static string UniqueEmail(string prefix = "member") => $"{prefix}-{Guid.NewGuid():N}@example.com";

    protected static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}";

    /// <summary>A whole-hour start time, <paramref name="daysAhead"/> days from now (UTC).</summary>
    protected static DateTimeOffset FutureSlot(int daysAhead = 7, int hour = 10)
    {
        var today = DateTimeOffset.UtcNow;
        return new DateTimeOffset(today.Year, today.Month, today.Day, hour, 0, 0, TimeSpan.Zero).AddDays(daysAhead);
    }

    protected async Task<MemberDto> RegisterMemberAsync(string? firstName = null, string? lastName = null, string? email = null)
    {
        var response = await Admin.Members.Register(new RegisterMemberCommand
        {
            FirstName = firstName ?? "Test",
            LastName = lastName ?? "Member",
            Email = email ?? UniqueEmail(),
        });

        return response.Value();
    }

    protected async Task<CourtDto> CreateCourtAsync(string? name = null, string surface = "glass")
    {
        var response = await Admin.Courts.Create(new CreateCourtCommand { Name = name ?? UniqueName("Court"), Surface = surface });
        return response.Value();
    }

    protected async Task<BookingDto> BookAsync(MemberDto member, CourtDto court, DateTimeOffset start, int minutes = 60)
    {
        var response = await As(member).Bookings.Create(new CreateBookingCommand
        {
            MemberId = member.Id,
            CourtId = court.Id,
            Start = start,
            End = start.AddMinutes(minutes),
        });

        return response.Value();
    }
}
