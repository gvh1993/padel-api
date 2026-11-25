using Padel.Application.Common;
using Padel.Application.Common.Querying;

namespace Padel.UnitTests.Application.Common;

public class SortMapTests
{
    private sealed record Item(string Name, int Rank);

    private static readonly SortMap<Item> Map = new SortMap<Item>("name")
        .Add("name", item => item.Name)
        .Add("rank", item => item.Rank);

    [Test]
    public async Task Given_NoSort_Resolve_Should_UseDefaultSort()
    {
        var sorts = Map.Resolve(null);

        await Assert.That(sorts).HasSingleItem();
        await Assert.That(sorts[0].Descending).IsFalse();
    }

    [Test]
    public async Task Given_MinusPrefix_Resolve_Should_SortDescending()
    {
        var sorts = Map.Resolve(" -RANK , name");

        await Assert.That(sorts.Select(s => s.Descending)).IsEquivalentTo([true, false], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task Given_DuplicateField_Resolve_Should_KeepFirstOccurrence()
    {
        var sorts = Map.Resolve("rank,-rank");

        await Assert.That(sorts).HasSingleItem();
        await Assert.That(sorts[0].Descending).IsFalse();
    }

    [Test]
    public async Task Given_UnknownField_FindUnknownFields_Should_ReturnIt()
    {
        await Assert.That(Map.FindUnknownFields("name,-secret")).IsEquivalentTo(["secret"]);
    }

    [Test]
    public async Task Given_UnknownField_Resolve_Should_ThrowBecauseTheValidatorMustRejectItFirst()
    {
        await Assert.That(() => Map.Resolve("secret")).Throws<InvalidOperationException>().WithMessageContaining("secret");
    }
}
