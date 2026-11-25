using FluentValidation;
using Padel.Domain.Common;

namespace Padel.Application.Features.Bookings;

public sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.CourtId).NotEmpty();
        RuleFor(x => x.Start).NotEmpty();
        RuleFor(x => x.End)
            .GreaterThan(x => x.Start)
            .Must((command, end) => end - command.Start >= TimeSlot.MinimumDuration && end - command.Start <= TimeSlot.MaximumDuration)
            .WithMessage($"A booking must last between {TimeSlot.MinimumDuration.TotalMinutes} minutes and {TimeSlot.MaximumDuration.TotalHours} hours.");
    }
}
