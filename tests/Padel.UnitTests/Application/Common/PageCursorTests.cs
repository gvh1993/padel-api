using System.Buffers.Text;
using System.Text;
using AwesomeAssertions;
using Padel.Application.Common.Querying;
using Padel.Application.Features.Bookings;
using Padel.Domain.Bookings;
using Padel.UnitTests.Builders;

namespace Padel.UnitTests.Application.Common;

public class PageCursorTests
{
    private static readonly IReadOnlyList<SortExpression<Booking>> ByStatusThenStartDescending = BookingSorting.Map.Resolve("status,-start");

    [Test]
    public void TryDecode_WhenEncodedForTheSameSort_ShouldReturnTheKeysAndId()
    {
        var booking = BookingBuilder.New().Cancelled().Generate();

        var cursor = PageCursor.Encode(booking, ByStatusThenStartDescending);

        PageCursor.TryDecode(cursor, ByStatusThenStartDescending, out var position).Should().BeTrue();
        position!.Id.Should().Be(booking.Id);
        // Enums are keyed by their position in the enum; dates keep their exact value.
        position.Keys.Should().Equal((int)BookingStatus.Cancelled, booking.Slot.Start);
    }

    [Test]
    public void Encode_ShouldBeUrlSafe()
    {
        var cursor = PageCursor.Encode(BookingBuilder.New().Generate(), ByStatusThenStartDescending);

        cursor.Should().MatchRegex("^[A-Za-z0-9_-]+$");
    }

    [Test]
    [Arguments("status,start")]
    [Arguments("status")]
    [Arguments("-start,status")]
    public void TryDecode_WhenEncodedForAnotherSort_ShouldFail(string otherSort)
    {
        var cursor = PageCursor.Encode(BookingBuilder.New().Generate(), ByStatusThenStartDescending);

        PageCursor.TryDecode(cursor, BookingSorting.Map.Resolve(otherSort), out _).Should().BeFalse();
    }

    [Test]
    [Arguments("")]
    [Arguments("not a cursor!")]
    [Arguments("e30")] // {}
    [Arguments("bnVsbA")] // null
    public void TryDecode_WhenNotACursor_ShouldFail(string cursor)
    {
        PageCursor.TryDecode(cursor, ByStatusThenStartDescending, out _).Should().BeFalse();
    }

    [Test]
    public void TryDecode_WhenAKeyHasTheWrongType_ShouldFail()
    {
        var json = """{"s":"status,-start","k":[1,"yesterday"],"id":"0199b5a4-0000-7000-8000-000000000000"}""";
        var cursor = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(json));

        PageCursor.TryDecode(cursor, ByStatusThenStartDescending, out _).Should().BeFalse();
    }
}
