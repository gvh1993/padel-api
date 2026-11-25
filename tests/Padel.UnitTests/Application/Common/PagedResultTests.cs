using AwesomeAssertions;
using Padel.Application.Common.Querying;
using Padel.Application.Features.Courts;
using Padel.Domain.Courts;
using Padel.UnitTests.Builders;

namespace Padel.UnitTests.Application.Common;

public class PagedResultTests
{
    private static readonly ListOptions<Court> TwoPerPage = new(2, CourtSorting.Map.Resolve("name"));

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(2)]
    public void FromWindow_WhenNoMoreThanThePageSize_ShouldBeTheLastPage(int count)
    {
        var window = CourtBuilder.New().Generate(count);

        var page = PagedResult.FromWindow(window, TwoPerPage);

        page.Items.Should().Equal(window);
        page.NextCursor.Should().BeNull();
    }

    [Test]
    public void FromWindow_WhenOneMoreThanThePageSize_ShouldDropItAndPointTheCursorAtTheLastItem()
    {
        var window = CourtBuilder.New().Generate(3);

        var page = PagedResult.FromWindow(window, TwoPerPage);

        page.Items.Should().Equal(window.Take(2));
        PageCursor.TryDecode(page.NextCursor!, TwoPerPage.Sorts, out var position).Should().BeTrue();
        position!.Id.Should().Be(window[1].Id);
        position.Keys.Should().Equal(window[1].Name);
    }

    [Test]
    public void Map_ShouldConvertItemsAndKeepTheCursor()
    {
        var page = PagedResult<int>.Create([1, 2], "next");

        var mapped = page.Map(number => number.ToString());

        mapped.Items.Should().Equal("1", "2");
        mapped.NextCursor.Should().Be("next");
    }
}
