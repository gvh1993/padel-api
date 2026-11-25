using Bogus;
using Padel.Domain.Common;
using Padel.Domain.Members;
using Soenneker.Utils.AutoBogus;

namespace Padel.UnitTests.Builders;

/// <summary>
/// <c>MemberBuilder.New().WithEmail("jan@example.com").Generate()</c>: an active member with a random name,
/// a unique email address and a mobile number, without pending domain events.
/// </summary>
internal static class MemberBuilder
{
    public static Faker<Member> New() =>
        new AutoFaker<Member>()
            // AutoFaker cannot call the private constructor. The rules below set every property, because
            // AutoFaker fills whatever has no rule with random values (private setters included).
            .CustomInstantiator(_ => Member.Register(
                FullName.Create("New", "Member").ShouldSucceed(), Email.Create("new.member@example.com").ShouldSucceed(), null, TestData.Now).ShouldSucceed())
            .RuleFor(m => m.Id, _ => Guid.CreateVersion7())
            .RuleFor(m => m.Version, 0u)
            .RuleFor(m => m.Name, f => FullName.Create(f.Name.FirstName(), f.Name.LastName()).ShouldSucceed())
            .RuleFor(m => m.Email, (f, m) => Email.Create(TestData.Email(f, m.Name.First, m.Name.Last)).ShouldSucceed())
            .RuleFor(m => m.Phone, f => TestData.MobileNumber(f))
            .RuleFor(m => m.Status, MemberStatus.Active)
            .RuleFor(m => m.SuspensionReason, _ => null)
            .RuleFor(m => m.RegisteredOn, TestData.Now.AddDays(-30))
            .RuleFor(m => m.IdentityId, _ => null)
            .FinishWith((_, m) => m.ClearDomainEvents());

    public static Faker<Member> WithName(this Faker<Member> faker, string firstName, string lastName) =>
        faker.RuleFor(m => m.Name, FullName.Create(firstName, lastName).ShouldSucceed());

    public static Faker<Member> WithEmail(this Faker<Member> faker, string email) =>
        faker.RuleFor(m => m.Email, Email.Create(email).ShouldSucceed());

    /// <summary>The phone number as stored: digits only.</summary>
    public static Faker<Member> WithPhone(this Faker<Member> faker, string? phone) =>
        faker.RuleFor(m => m.Phone, _ => phone);

    public static Faker<Member> RegisteredOn(this Faker<Member> faker, DateTimeOffset registeredOn) =>
        faker.RuleFor(m => m.RegisteredOn, registeredOn);

    public static Faker<Member> Suspended(this Faker<Member> faker, string? reason = null) =>
        faker
            .RuleFor(m => m.Status, MemberStatus.Suspended)
            .RuleFor(m => m.SuspensionReason, _ => reason);

    /// <summary>Linked to an account at the identity provider (the token's <c>sub</c>).</summary>
    public static Faker<Member> LinkedTo(this Faker<Member> faker, string identityId) =>
        faker.RuleFor(m => m.IdentityId, identityId);
}
