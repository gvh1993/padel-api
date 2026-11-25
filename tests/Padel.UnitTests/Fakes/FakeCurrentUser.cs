using Padel.Application.Common;

namespace Padel.UnitTests.Fakes;

public sealed class FakeCurrentUser : ICurrentUser
{
    public string? UserId { get; set; } = "test-user";

    public Guid? MemberId { get; set; }

    public bool IsAdmin { get; set; }

    public static FakeCurrentUser Admin() => new() { IsAdmin = true };

    public static FakeCurrentUser ForMember(Guid memberId) => new() { MemberId = memberId };
}
