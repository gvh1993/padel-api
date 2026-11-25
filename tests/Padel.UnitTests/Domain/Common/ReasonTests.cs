using AwesomeAssertions;
using Padel.Domain.Common;

namespace Padel.UnitTests.Domain.Common;

public class ReasonTests
{
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public void Create_WhenBlankReason_ShouldReturnNone(string? reason)
    {
        Reason.Create(reason).ShouldSucceed().Should().Be(Reason.None);
    }

    [Test]
    public void Create_ShouldTrimTheReason()
    {
        Reason.Create("  rain  ").ShouldSucceed().Text.Should().Be("rain");
    }

    [Test]
    public void Create_WhenReasonLongerThanMaxLength_ShouldFail()
    {
        Reason.Create(new string('x', Reason.MaxLength + 1)).ShouldFail();
    }
}
