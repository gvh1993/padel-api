using FluentValidation;
using Padel.Application.Common.Querying;
using Padel.Domain.Courts;

namespace Padel.Application.Features.Courts;

public sealed class ListCourtsQueryValidator : ListQueryValidator<ListCourtsQuery, Court>
{
    public ListCourtsQueryValidator() : base(CourtSorting.Map)
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Surface).MaximumLength(Court.MaxLength);
        RuleFor(x => x.Search).MaximumLength(Court.MaxLength);
    }
}
