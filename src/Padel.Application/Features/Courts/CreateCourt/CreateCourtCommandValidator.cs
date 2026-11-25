using FluentValidation;
using Padel.Application.Common.Validation;

namespace Padel.Application.Features.Courts;

public sealed class CreateCourtCommandValidator : AbstractValidator<CreateCourtCommand>
{
    public CreateCourtCommandValidator()
    {
        RuleFor(x => x.Name).ValidCourtText();
        RuleFor(x => x.Surface).ValidCourtText();
    }
}
