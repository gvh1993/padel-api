using FluentValidation;
using Padel.Application.Common.Validation;

namespace Padel.Application.Features.Members;

public sealed class UpdateMemberProfileCommandValidator : AbstractValidator<UpdateMemberProfileCommand>
{
    public UpdateMemberProfileCommandValidator()
    {
        RuleFor(x => x.FirstName).ValidPersonName();
        RuleFor(x => x.LastName).ValidPersonName();
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Phone).ValidPhone();
    }
}
