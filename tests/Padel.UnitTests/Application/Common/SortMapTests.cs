using AwesomeAssertions;
using Padel.Application.Common.Querying;

namespace Padel.UnitTests.Application.Common;

public class SortMapTests
{
    private sealed record Item(string Name, int Rank);

    private static readonly SortMap<Item> Map = new SortMap<Item>("name")
        .Add("name", item => item.Name)
        .Add("rank", item => item.Rank);

    [Test]
    public void Given_NoSort_Resolve_Should_UseDefaultSort()
    {
        var sorts = Map.Resolve(null);

        sorts.Should().ContainSingle();
        sorts[0].Descending.Should().BeFalse();
    }

    [Test]
    public void Given_MinusPrefix_Resolve_Should_SortDescending()
    {
        var sorts = Map.Resolve(" -RANK , name");

        sorts.Select(s => s.Descending).Should().Equal(new[] { true, false });
    }

    [Test]
    public void Given_DuplicateField_Resolve_Should_KeepFirstOccurrence()
    {
        var sorts = Map.Resolve("rank,-rank");

        sorts.Should().ContainSingle();
        sorts[0].Descending.Should().BeFalse();
    }

    [Test]
    public void Given_UnknownField_FindUnknownFields_Should_ReturnIt()
    {
        Map.FindUnknownFields("name,-secret").Should().BeEquivalentTo(new[] { "secret" });
    }

    [Test]
    public void Given_UnknownField_Resolve_Should_ThrowBecauseTheValidatorMustRejectItFirst()
    {
        FluentActions.Invoking(() => Map.Resolve("secret")).Should().Throw<InvalidOperationException>().WithMessage("*secret*");
    }
}
