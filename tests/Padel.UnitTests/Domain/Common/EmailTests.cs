using AwesomeAssertions;
using Padel.Domain.Common;

namespace Padel.UnitTests.Domain.Common;

public class EmailTests
{
    [Test]
    public void Create_Should_TrimAndLowercaseTheAddress()
    {
        var email = Email.Create("  John.Doe@Example.COM ").ShouldSucceed();

        email.Value.Should().Be("john.doe@example.com");
    }

    [Test]
    public void Given_SameAddressWithDifferentCasing_Should_BeEqual()
    {
        Email.Create("A@Example.com").ShouldSucceed().Should().Be(Email.Create("a@example.com").ShouldSucceed());
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("no-at-sign")]
    [Arguments("two@@example.com")]
    [Arguments("missing@tld")]
    [Arguments("@domain.com")]
    [Arguments("user@")]
    [Arguments("user@.com")]
    [Arguments("user@domain.")]
    public void Given_InvalidAddress_Should_Fail(string value)
    {
        Email.Create(value).ShouldFail();
    }

    [Test]
    public void Given_AddressLongerThanMaxLength_Should_Fail()
    {
        var value = new string('a', 64) + "@" + new string('b', Email.MaxLength) + ".com";

        Email.Create(value).ShouldFail();
    }
}
