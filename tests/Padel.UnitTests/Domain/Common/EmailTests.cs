using Padel.Domain.Common;

namespace Padel.UnitTests.Domain.Common;

public class EmailTests
{
    [Test]
    public async Task Create_Should_TrimAndLowercaseTheAddress()
    {
        var email = new Email("  John.Doe@Example.COM ");

        await Assert.That(email.Value).IsEqualTo("john.doe@example.com");
    }

    [Test]
    public async Task Given_SameAddressWithDifferentCasing_Should_BeEqual()
    {
        await Assert.That(new Email("A@Example.com")).IsEqualTo(new Email("a@example.com"));
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
    public async Task Given_InvalidAddress_Should_ThrowDomainException(string value)
    {
        await Assert.That(() => new Email(value)).Throws<DomainException>();
    }

    [Test]
    public async Task Given_AddressLongerThanMaxLength_Should_ThrowDomainException()
    {
        var value = new string('a', 64) + "@" + new string('b', Email.MaxLength) + ".com";

        await Assert.That(() => new Email(value)).Throws<DomainException>();
    }
}
