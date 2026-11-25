using FluentValidation;
using Padel.Application.Common.Querying;
using Padel.Domain.Members;

namespace Padel.Application.Features.Members;

public sealed class ListMembersQueryValidator : ListQueryValidator<ListMembersQuery, Member>
{
    public ListMembersQueryValidator() : base(MemberSorting.Map)
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Search).MaximumLength(100);
    }
}
