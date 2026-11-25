using AwesomeAssertions;
using Padel.Application.Features.Bookings.Notifications;
using Padel.Domain.Bookings;
using Padel.UnitTests.Fakes;

namespace Padel.UnitTests.Application.Bookings;

public class BookingNotificationTests
{
    // 26 November 2026, 10:00 UTC = 11:00 in Amsterdam.
    private static readonly DateTimeOffset Tomorrow10 = new(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);

    private readonly HandlerFixture _fixture = new();
    private readonly RecordingNotificationSender _sender = new();

    private BookingNotifications Notifications =>
        new(_fixture.Bookings, _fixture.Members, _fixture.Courts, _sender, _fixture.PolicyOptions);

    [Test]
    public async Task Given_BookingCreated_SendBookingConfirmation_Should_MailTheMemberInClubTime()
    {
        var member = _fixture.AddMember("jan@example.com");
        var court = _fixture.AddCourt("Centre Court");
        var booking = _fixture.AddBooking(member, court, Tomorrow10);

        await new SendBookingConfirmation(Notifications).Handle(new BookingCreated(booking.Id, member.Id, court.Id), CancellationToken.None);

        var notification = _sender.Sent.Should().ContainSingle().Subject;
        notification.To.Should().Be("jan@example.com");
        notification.Subject.Should().Be("Booking confirmed");
        notification.Body.Should().Contain("Centre Court").And.Contain("from 11:00 to 12:00");
    }

    [Test]
    public async Task Given_BookingCancelled_SendBookingCancellation_Should_MailTheMember()
    {
        var member = _fixture.AddMember("jan@example.com");
        var booking = _fixture.AddBooking(member, _fixture.AddCourt(), Tomorrow10);

        await new SendBookingCancellation(Notifications).Handle(new BookingCancelled(booking.Id, "rain"), CancellationToken.None);

        _sender.Sent.Should().ContainSingle().Which.Subject.Should().Be("Booking cancelled");
    }

    [Test]
    public async Task Given_BookingThatNoLongerExists_SendBookingConfirmation_Should_SendNothing()
    {
        await new SendBookingConfirmation(Notifications).Handle(new BookingCreated(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        _sender.Sent.Should().BeEmpty();
    }
}
