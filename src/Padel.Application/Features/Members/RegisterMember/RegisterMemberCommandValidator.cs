using FluentValidation;
using Padel.Application.Common.Validation;

namespace Padel.Application.Features.Members;

public sealed class RegisterMemberCommandValidator : AbstractValidator<RegisterMemberCommand>
{
    public RegisterMemberCommandValidator()
    {
        RuleFor(x => x.FirstName).ValidFirstName();
        RuleFor(x => x.LastName).ValidLastName();
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Phone).ValidPhone();
    }
}
