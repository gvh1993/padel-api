using Bogus;
using Padel.Api.Features.Courts;
using Soenneker.Utils.AutoBogus;

namespace Padel.IntegrationTests.Builders;

/// <summary><c>CreateCourtRequestBuilder.New().WithName(name).Generate()</c>: a court with a unique name and a random surface.</summary>
internal static class CreateCourtRequestBuilder
{
    private static readonly string[] Surfaces = ["glass", "panoramic glass", "wall", "mesh"];

    public static Faker<CreateCourtRequest> New() =>
        new AutoFaker<CreateCourtRequest>()
            .RuleFor(r => r.Name, _ => $"Baan {Guid.NewGuid():N}")
            .RuleFor(r => r.Surface, f => f.PickRandom(Surfaces));

    public static Faker<CreateCourtRequest> WithName(this Faker<CreateCourtRequest> faker, string name) =>
        faker.RuleFor(r => r.Name, name);

    public static Faker<CreateCourtRequest> WithSurface(this Faker<CreateCourtRequest> faker, string surface) =>
        faker.RuleFor(r => r.Surface, surface);
}
