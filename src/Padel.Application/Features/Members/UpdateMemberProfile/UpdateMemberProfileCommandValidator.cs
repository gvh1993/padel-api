using FluentValidation;
using Padel.Application.Common.Validation;

namespace Padel.Application.Features.Members;

public sealed class UpdateMemberProfileCommandValidator : AbstractValidator<UpdateMemberProfileCommand>
{
    public UpdateMemberProfileCommandValidator()
    {
        RuleFor(x => x.FirstName).ValidFirstName();
        RuleFor(x => x.LastName).ValidLastName();
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Phone).ValidPhone();
    }
}
