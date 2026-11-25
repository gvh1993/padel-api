using Padel.Domain.Common;

namespace Padel.UnitTests.Domain.Common;

public class EmailTests
{
    [Test]
    public async Task Create_Should_TrimAndLowercaseTheAddress()
    {
        var email = Email.Create("  John.Doe@Example.COM ").ShouldSucceed();

        await Assert.That(email.Value).IsEqualTo("john.doe@example.com");
    }

    [Test]
    public async Task Given_SameAddressWithDifferentCasing_Should_BeEqual()
    {
        await Assert.That(Email.Create("A@Example.com").ShouldSucceed()).IsEqualTo(Email.Create("a@example.com").ShouldSucceed());
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
    public async Task Given_InvalidAddress_Should_Fail(string value)
    {
        await Email.Create(value).ShouldFail();
    }

    [Test]
    public async Task Given_AddressLongerThanMaxLength_Should_Fail()
    {
        var value = new string('a', 64) + "@" + new string('b', Email.MaxLength) + ".com";

        await Email.Create(value).ShouldFail();
    }
}
