using Bogus;
using Padel.Domain.Courts;
using Soenneker.Utils.AutoBogus;

namespace Padel.UnitTests.Builders;

/// <summary><c>CourtBuilder.New().WithName("Court A").Generate()</c>: an available court with a random name and surface.</summary>
internal static class CourtBuilder
{
    private static readonly string[] Surfaces = ["glass", "panoramic glass", "wall", "mesh"];

    public static Faker<Court> New() =>
        new AutoFaker<Court>()
            // AutoFaker cannot call the private constructor; the rules below set every property.
            .CustomInstantiator(_ => Court.Create("New court", "glass").ShouldSucceed())
            .RuleFor(c => c.Id, _ => Guid.CreateVersion7())
            .RuleFor(c => c.Version, 0u)
            .RuleFor(c => c.Name, f => $"Baan {f.Random.Number(1, 99)}")
            .RuleFor(c => c.Surface, f => f.PickRandom(Surfaces))
            .RuleFor(c => c.Status, CourtStatus.Available)
            .FinishWith((_, c) => c.ClearDomainEvents());

    public static Faker<Court> WithName(this Faker<Court> faker, string name) =>
        faker.RuleFor(c => c.Name, name);

    public static Faker<Court> WithSurface(this Faker<Court> faker, string surface) =>
        faker.RuleFor(c => c.Surface, surface);

    public static Faker<Court> InMaintenance(this Faker<Court> faker) =>
        faker.RuleFor(c => c.Status, CourtStatus.Maintenance);

    public static Faker<Court> Retired(this Faker<Court> faker) =>
        faker.RuleFor(c => c.Status, CourtStatus.Retired);
}
