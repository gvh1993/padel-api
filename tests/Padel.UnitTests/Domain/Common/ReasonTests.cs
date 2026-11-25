using AwesomeAssertions;
using Padel.Domain.Common;

namespace Padel.UnitTests.Domain.Common;

public class ReasonTests
{
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public void Given_BlankReason_Create_Should_ReturnNone(string? reason)
    {
        Reason.Create(reason).ShouldSucceed().Should().Be(Reason.None);
    }

    [Test]
    public void Create_Should_TrimTheReason()
    {
        Reason.Create("  rain  ").ShouldSucceed().Text.Should().Be("rain");
    }

    [Test]
    public void Given_ReasonLongerThanMaxLength_Should_Fail()
    {
        Reason.Create(new string('x', Reason.MaxLength + 1)).ShouldFail();
    }
}
