using FluentValidation;
using Padel.Application.Common.Validation;

namespace Padel.Application.Features.Bookings;

public sealed class CancelBookingCommandValidator : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingCommandValidator()
    {
        RuleFor(x => x.Reason).ValidReason();
    }
}
