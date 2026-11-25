using Padel.Domain.Common;

namespace Padel.UnitTests.Domain.Common;

public class ReasonTests
{
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Given_BlankReason_Should_ReturnNull(string? reason)
    {
        await Assert.That(Reason.Normalize(reason)).IsNull();
    }

    [Test]
    public async Task Normalize_Should_TrimTheReason()
    {
        await Assert.That(Reason.Normalize("  rain  ")).IsEqualTo("rain");
    }

    [Test]
    public async Task Given_ReasonLongerThanMaxLength_Should_ThrowDomainException()
    {
        await Assert.That(() => Reason.Normalize(new string('x', Reason.MaxLength + 1))).Throws<DomainException>();
    }
}
