using AwesomeAssertions;
using Padel.Domain.Common;

namespace Padel.UnitTests.Domain.Common;

public class FullNameTests
{
    [Test]
    public void Create_ShouldTrimBothParts()
    {
        var name = FullName.Create("  Gert-Jan ", "  De Vries  ").ShouldSucceed();

        name.First.Should().Be("Gert-Jan");
        name.Last.Should().Be("De Vries");
        name.DisplayName.Should().Be("Gert-Jan De Vries");
    }

    [Test]
    public void Create_WhenSameParts_ShouldBeEqual()
    {
        FullName.Create("Jan", "Jansen").ShouldSucceed().Should().Be(FullName.Create(" Jan", "Jansen ").ShouldSucceed());
    }

    [Test]
    [Arguments("", "Vries")]
    [Arguments("Smith", "")]
    [Arguments("  ", "  ")]
    public void Create_WhenBlankPart_ShouldFail(string first, string last)
    {
        FullName.Create(first, last).ShouldFail();
    }

    [Test]
    public void Create_WhenPartLongerThanMaxLength_ShouldFail()
    {
        var tooLong = new string('a', FullName.MaxLength + 1);

        FullName.Create(tooLong, "Vries").ShouldFail();
        FullName.Create("Smith", tooLong).ShouldFail();
    }
}
