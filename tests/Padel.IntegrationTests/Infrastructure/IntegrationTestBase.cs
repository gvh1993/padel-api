using Bogus;
using Padel.Api.Features.Bookings;
using Padel.Api.Features.Courts;
using Padel.Api.Features.Members;

using Padel.IntegrationTests.Builders;

namespace Padel.IntegrationTests.Infrastructure;

/// <summary>
/// Callers and test data helpers. Every test creates its own members and courts (unique emails and
/// names), so tests can share the database and run in parallel.
/// </summary>
public abstract class IntegrationTestBase(PadelApiFactory factory)
{
    protected PadelApi Admin { get; } = factory.CreateApi(() => factory.Tokens.AdminAsync());

    protected PadelApi Anonymous { get; } = factory.CreateApi();

    /// <summary>A Keycloak account with the member's (verified) email address, which the API links to the member.</summary>
    protected PadelApi As(RegisterMemberResponse member) => factory.CreateApi(() => factory.Tokens.ForEmailAsync(member.Email));

    /// <summary>A Keycloak user that is not linked to a member.</summary>
    protected PadelApi Unlinked { get; } = factory.CreateApi(() => factory.Tokens.WithoutMemberLinkAsync());

    protected PadelApi WithToken(string token) => factory.CreateApi(() => Task.FromResult(token));

    protected KeycloakTokens Tokens => factory.Tokens;

    protected RecordingNotificationSender Notifications => factory.Notifications;

    protected static string UniqueEmail(string prefix = "member") => $"{prefix}-{Guid.NewGuid():N}@example.com";

    protected static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}";

    /// <summary>A whole-hour start time, <paramref name="daysAhead"/> days from now (UTC).</summary>
    protected static DateTimeOffset FutureSlot(int daysAhead = 7, int hour = 10) => TestData.FutureSlot(daysAhead, hour);

    /// <summary>Registers a member as admin: a random one, or the one <paramref name="request"/> describes.</summary>
    protected async Task<RegisterMemberResponse> RegisterMemberAsync(Faker<RegisterMemberRequest>? request = null)
    {
        var response = await Admin.Members.Register((request ?? RegisterMemberRequestBuilder.New()).Generate());
        return response.Value();
    }

    /// <summary>Creates a court as admin: a random one, or the one <paramref name="request"/> describes.</summary>
    protected async Task<CreateCourtResponse> CreateCourtAsync(Faker<CreateCourtRequest>? request = null)
    {
        var response = await Admin.Courts.Create((request ?? CreateCourtRequestBuilder.New()).Generate());
        return response.Value();
    }

    /// <summary>Books as the member.</summary>
    protected async Task<CreateBookingResponse> BookAsync(RegisterMemberResponse member, CreateCourtResponse court, DateTimeOffset start, int minutes = 60)
    {
        var request = CreateBookingRequestBuilder.New().For(member.Id).On(court.Id).StartingAt(start, minutes).Generate();
        var response = await As(member).Bookings.Create(request);
        return response.Value();
    }
}
