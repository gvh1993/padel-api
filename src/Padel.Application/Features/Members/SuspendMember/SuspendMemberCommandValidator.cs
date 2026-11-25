using FluentValidation;
using Padel.Application.Common.Validation;

namespace Padel.Application.Features.Members;

public sealed class SuspendMemberCommandValidator : AbstractValidator<SuspendMemberCommand>
{
    public SuspendMemberCommandValidator()
    {
        RuleFor(x => x.Reason).ValidReason();
    }
}
