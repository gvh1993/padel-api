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
        RuleFor(x => x.End).Custom((end, context) =>
        {
            if (TimeSlot.Create(context.InstanceToValidate.Start, end) is Error error)
            {
                context.AddFailure(error.Description);
            }
        });
    }
}
