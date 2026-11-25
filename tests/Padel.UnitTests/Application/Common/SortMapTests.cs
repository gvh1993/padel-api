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
    public void Resolve_WhenNoSort_ShouldUseDefaultSort()
    {
        var sorts = Map.Resolve(null);

        sorts.Should().ContainSingle();
        sorts[0].Descending.Should().BeFalse();
    }

    [Test]
    public void Resolve_WhenMinusPrefix_ShouldSortDescending()
    {
        var sorts = Map.Resolve(" -RANK , name");

        sorts.Select(s => s.Descending).Should().Equal(new[] { true, false });
    }

    [Test]
    public void Resolve_WhenDuplicateField_ShouldKeepFirstOccurrence()
    {
        var sorts = Map.Resolve("rank,-rank");

        sorts.Should().ContainSingle();
        sorts[0].Descending.Should().BeFalse();
    }

    [Test]
    public void FindUnknownFields_WhenUnknownField_ShouldReturnIt()
    {
        Map.FindUnknownFields("name,-secret").Should().BeEquivalentTo(new[] { "secret" });
    }

    [Test]
    public void Resolve_WhenUnknownField_ShouldThrowBecauseTheValidatorMustRejectItFirst()
    {
        FluentActions.Invoking(() => Map.Resolve("secret")).Should().Throw<InvalidOperationException>().WithMessage("*secret*");
    }
}
