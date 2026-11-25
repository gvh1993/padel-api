using FluentValidation;

namespace Padel.Application.Common.Querying;

/// <summary>
/// Shared paging and sorting rules for list queries.
/// </summary>
public abstract class ListQueryValidator<TQuery, TEntity> : AbstractValidator<TQuery>
    where TQuery : ListQuery
{
    protected ListQueryValidator(SortMap<TEntity> sortMap)
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, ListQuery.MaxPageSize);
        RuleFor(x => x.Sort)
            .Must(sort => sortMap.FindUnknownFields(sort).Count == 0)
            .WithMessage(query =>
                $"Unknown sort field(s): {string.Join(", ", sortMap.FindUnknownFields(query.Sort))}. " +
                $"Allowed fields: {string.Join(", ", sortMap.FieldNames)}.");
    }
}
