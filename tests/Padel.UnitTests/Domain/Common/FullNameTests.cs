using Padel.Domain.Common;

namespace Padel.UnitTests.Domain.Common;

public class FullNameTests
{
    [Test]
    public async Task Create_Should_TrimBothParts()
    {
        var name = new FullName("  Gert-Jan ", "  De Vries  ");

        await Assert.That(name.First).IsEqualTo("Gert-Jan");
        await Assert.That(name.Last).IsEqualTo("De Vries");
        await Assert.That(name.DisplayName).IsEqualTo("Gert-Jan De Vries");
    }

    [Test]
    public async Task Given_SameParts_Should_BeEqual()
    {
        await Assert.That(new FullName("Jan", "Jansen")).IsEqualTo(new FullName(" Jan", "Jansen "));
    }

    [Test]
    [Arguments("", "Vries")]
    [Arguments("Smith", "")]
    [Arguments("  ", "  ")]
    public async Task Given_BlankPart_Should_ThrowDomainException(string first, string last)
    {
        await Assert.That(() => new FullName(first, last)).Throws<DomainException>();
    }

    [Test]
    public async Task Given_PartLongerThanMaxLength_Should_ThrowDomainException()
    {
        var tooLong = new string('a', FullName.MaxLength + 1);

        await Assert.That(() => new FullName(tooLong, "Vries")).Throws<DomainException>();
        await Assert.That(() => new FullName("Smith", tooLong)).Throws<DomainException>();
    }
}
