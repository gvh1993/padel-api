using FluentValidation;
using Padel.Application.Common.Validation;

namespace Padel.Application.Features.Courts;

public sealed class RenameCourtCommandValidator : AbstractValidator<RenameCourtCommand>
{
    public RenameCourtCommandValidator()
    {
        RuleFor(x => x.Name).ValidCourtText();
    }
}
