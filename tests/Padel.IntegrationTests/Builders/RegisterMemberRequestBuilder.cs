using Bogus;
using Padel.Api.Features.Members;
using Soenneker.Utils.AutoBogus;

namespace Padel.IntegrationTests.Builders;

/// <summary><c>RegisterMemberRequestBuilder.New().WithEmail(email).Generate()</c>: a valid registration with a unique email address.</summary>
internal static class RegisterMemberRequestBuilder
{
    public static Faker<RegisterMemberRequest> New() =>
        new AutoFaker<RegisterMemberRequest>()
            .RuleFor(r => r.FirstName, f => f.Name.FirstName())
            .RuleFor(r => r.LastName, f => f.Name.LastName())
            .RuleFor(r => r.Email, (f, r) => TestData.Email(f, r.FirstName, r.LastName))
            .RuleFor(r => r.Phone, f => TestData.MobileNumber(f));

    public static Faker<RegisterMemberRequest> WithName(this Faker<RegisterMemberRequest> faker, string firstName, string lastName) =>
        faker
            .RuleFor(r => r.FirstName, firstName)
            .RuleFor(r => r.LastName, lastName);

    public static Faker<RegisterMemberRequest> WithEmail(this Faker<RegisterMemberRequest> faker, string email) =>
        faker.RuleFor(r => r.Email, email);

    public static Faker<RegisterMemberRequest> WithPhone(this Faker<RegisterMemberRequest> faker, string? phone) =>
        faker.RuleFor(r => r.Phone, _ => phone);
}
