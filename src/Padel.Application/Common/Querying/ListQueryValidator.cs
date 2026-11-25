using FluentValidation;
using Padel.Domain.Common;

namespace Padel.Application.Common.Querying;

/// <summary>
/// Shared paging and sorting rules for list queries.
/// </summary>
public abstract class ListQueryValidator<TQuery, TEntity> : AbstractValidator<TQuery>
    where TQuery : ListQuery
    where TEntity : Entity
{
    protected ListQueryValidator(SortMap<TEntity> sortMap)
    {
        RuleFor(x => x.PageSize).InclusiveBetween(1, ListQuery.MaxPageSize);
        RuleFor(x => x.Sort)
            .Must(sort => sortMap.FindUnknownFields(sort).Count == 0)
            .WithMessage(query =>
                $"Unknown sort field(s): {string.Join(", ", sortMap.FindUnknownFields(query.Sort))}. " +
                $"Allowed fields: {string.Join(", ", sortMap.FieldNames)}.");

        // Only checked with a valid sort: a cursor belongs to one sort order.
        RuleFor(x => x.Cursor)
            .Cascade(CascadeMode.Stop)
            .MaximumLength(PageCursor.MaxLength)
            .Must((query, cursor) => PageCursor.TryDecode(cursor!, sortMap.Resolve(query.Sort), out _))
            .WithMessage("The cursor is invalid or was made for another sort order. Start again without a cursor.")
            .When(query => query.Cursor is not null && sortMap.FindUnknownFields(query.Sort).Count == 0);
    }
}
