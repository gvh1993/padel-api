using Padel.Application.Common.Querying;

namespace Padel.UnitTests.Application.Common;

public class PagedResultTests
{
    [Test]
    [Arguments(0, 10, 0)]
    [Arguments(1, 10, 1)]
    [Arguments(10, 10, 1)]
    [Arguments(11, 10, 2)]
    public async Task Create_Should_CalculateTotalPages(int totalCount, int pageSize, int expectedPages)
    {
        var page = PagedResult<int>.Create([], 1, pageSize, totalCount);

        await Assert.That(page.TotalPages).IsEqualTo(expectedPages);
    }

    [Test]
    public async Task Map_Should_ConvertItemsAndKeepPagingInfo()
    {
        var page = PagedResult<int>.Create([1, 2], 2, 2, 5);

        var mapped = page.Map(number => number.ToString());

        await Assert.That(mapped.Items).IsEquivalentTo(["1", "2"]);
        await Assert.That(mapped.Page).IsEqualTo(2);
        await Assert.That(mapped.TotalCount).IsEqualTo(5);
        await Assert.That(mapped.TotalPages).IsEqualTo(3);
    }
}
