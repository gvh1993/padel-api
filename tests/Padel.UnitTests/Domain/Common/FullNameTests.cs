using Padel.Domain.Common;

namespace Padel.UnitTests.Domain.Common;

public class FullNameTests
{
    [Test]
    public async Task Create_Should_TrimBothParts()
    {
        var name = FullName.Create("  Gert-Jan ", "  De Vries  ").ShouldSucceed();

        await Assert.That(name.First).IsEqualTo("Gert-Jan");
        await Assert.That(name.Last).IsEqualTo("De Vries");
        await Assert.That(name.DisplayName).IsEqualTo("Gert-Jan De Vries");
    }

    [Test]
    public async Task Given_SameParts_Should_BeEqual()
    {
        await Assert.That(FullName.Create("Jan", "Jansen").ShouldSucceed()).IsEqualTo(FullName.Create(" Jan", "Jansen ").ShouldSucceed());
    }

    [Test]
    [Arguments("", "Vries")]
    [Arguments("Smith", "")]
    [Arguments("  ", "  ")]
    public async Task Given_BlankPart_Should_Fail(string first, string last)
    {
        await FullName.Create(first, last).ShouldFail();
    }

    [Test]
    public async Task Given_PartLongerThanMaxLength_Should_Fail()
    {
        var tooLong = new string('a', FullName.MaxLength + 1);

        await FullName.Create(tooLong, "Vries").ShouldFail();
        await FullName.Create("Smith", tooLong).ShouldFail();
    }
}
