using Padel.Domain.Common;

namespace Padel.UnitTests.Domain.Common;

public class ReasonTests
{
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Given_BlankReason_Create_Should_ReturnNone(string? reason)
    {
        await Assert.That(Reason.Create(reason).ShouldSucceed()).IsEqualTo(Reason.None);
    }

    [Test]
    public async Task Create_Should_TrimTheReason()
    {
        await Assert.That(Reason.Create("  rain  ").ShouldSucceed().Text).IsEqualTo("rain");
    }

    [Test]
    public async Task Given_ReasonLongerThanMaxLength_Should_Fail()
    {
        await Reason.Create(new string('x', Reason.MaxLength + 1)).ShouldFail();
    }
}
