using AwesomeAssertions;
using Padel.Application.Common.Querying;

namespace Padel.UnitTests.Application.Common;

public class PagedResultTests
{
    [Test]
    [Arguments(0, 10, 0)]
    [Arguments(1, 10, 1)]
    [Arguments(10, 10, 1)]
    [Arguments(11, 10, 2)]
    public void Create_ShouldCalculateTotalPages(int totalCount, int pageSize, int expectedPages)
    {
        var page = PagedResult<int>.Create([], 1, pageSize, totalCount);

        page.TotalPages.Should().Be(expectedPages);
    }

    [Test]
    public void Map_ShouldConvertItemsAndKeepPagingInfo()
    {
        var page = PagedResult<int>.Create([1, 2], 2, 2, 5);

        var mapped = page.Map(number => number.ToString());

        mapped.Items.Should().BeEquivalentTo(new[] { "1", "2" });
        mapped.Page.Should().Be(2);
        mapped.TotalCount.Should().Be(5);
        mapped.TotalPages.Should().Be(3);
    }
}
