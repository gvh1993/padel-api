using NSubstitute;
using Padel.Application.Common.Notifications;
using Padel.Application.Features.Bookings.Notifications;
using Padel.Domain.Bookings;
using Padel.UnitTests.Builders;
using Padel.UnitTests.Fakes;

namespace Padel.UnitTests.Application.Bookings;

public class BookingNotificationTests
{
    // 26 November 2026, 10:00 UTC = 11:00 in Amsterdam.
    private static readonly DateTimeOffset Tomorrow10 = new(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);

    private readonly HandlerFixture _fixture = new();
    private readonly INotificationSender _sender = Substitute.For<INotificationSender>();

    private BookingNotifications Notifications =>
        new(_fixture.Bookings, _fixture.Members, _fixture.Courts, _sender, _fixture.PolicyOptions);

    [Test]
    public async Task SendBookingConfirmation_WhenBookingCreated_ShouldMailTheMemberInClubTime()
    {
        var member = _fixture.Add(MemberBuilder.New().WithEmail("jan@example.com"));
        var court = _fixture.Add(CourtBuilder.New().WithName("Centre Court"));
        var booking = _fixture.Add(BookingBuilder.New().For(member).On(court).StartingAt(Tomorrow10));

        await new SendBookingConfirmation(Notifications).Handle(new BookingCreated(booking.Id, member.Id, court.Id), CancellationToken.None);

        await _sender.Received(1).SendAsync(
            Arg.Is<Notification>(n =>
                n.To == "jan@example.com"
                && n.Subject == "Booking confirmed"
                && n.Body.Contains("Centre Court")
                && n.Body.Contains("from 11:00 to 12:00")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendBookingCancellation_WhenBookingCancelled_ShouldMailTheMember()
    {
        var member = _fixture.Add(MemberBuilder.New().WithEmail("jan@example.com"));
        var booking = _fixture.Add(BookingBuilder.New().For(member).On(_fixture.Add(CourtBuilder.New())).StartingAt(Tomorrow10));

        await new SendBookingCancellation(Notifications).Handle(new BookingCancelled(booking.Id, "rain"), CancellationToken.None);

        await _sender.Received(1).SendAsync(Arg.Is<Notification>(n => n.Subject == "Booking cancelled"), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SendBookingConfirmation_GivenBookingThatNoLongerExists_ShouldSendNothing()
    {
        await new SendBookingConfirmation(Notifications).Handle(new BookingCreated(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await _sender.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }
}
