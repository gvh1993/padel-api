using AwesomeAssertions;
using Padel.Domain.Common;

namespace Padel.UnitTests.Domain.Common;

public class FullNameTests
{
    [Test]
    public void Create_Should_TrimBothParts()
    {
        var name = FullName.Create("  Gert-Jan ", "  De Vries  ").ShouldSucceed();

        name.First.Should().Be("Gert-Jan");
        name.Last.Should().Be("De Vries");
        name.DisplayName.Should().Be("Gert-Jan De Vries");
    }

    [Test]
    public void Given_SameParts_Should_BeEqual()
    {
        FullName.Create("Jan", "Jansen").ShouldSucceed().Should().Be(FullName.Create(" Jan", "Jansen ").ShouldSucceed());
    }

    [Test]
    [Arguments("", "Vries")]
    [Arguments("Smith", "")]
    [Arguments("  ", "  ")]
    public void Given_BlankPart_Should_Fail(string first, string last)
    {
        FullName.Create(first, last).ShouldFail();
    }

    [Test]
    public void Given_PartLongerThanMaxLength_Should_Fail()
    {
        var tooLong = new string('a', FullName.MaxLength + 1);

        FullName.Create(tooLong, "Vries").ShouldFail();
        FullName.Create("Smith", tooLong).ShouldFail();
    }
}
