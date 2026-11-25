using Padel.Application.Common.Messaging;
using Padel.Application.Common.Querying;
using Padel.Domain.Members;

namespace Padel.Application.Features.Members;

public sealed record ListMembersQuery : ListQuery, IRequest<PagedResult<MemberDto>>
{
    public MemberStatus? Status { get; init; }

    /// <summary>Case-insensitive match on first name, last name or email.</summary>
    public string? Search { get; init; }
}

public sealed class ListMembersQueryHandler(IMemberRepository members)
    : IRequestHandler<ListMembersQuery, PagedResult<MemberDto>>
{
    public async Task<PagedResult<MemberDto>> Handle(ListMembersQuery request, CancellationToken cancellationToken)
    {
        var status = request.Status;
        var search = request.Search?.Trim().ToLowerInvariant();

        var options = ListOptions<Member>.From(request, MemberSorting.Map)
            .WhereIf(status.HasValue, member => member.Status == status)
            .WhereIf(!string.IsNullOrEmpty(search), member =>
                member.Name.First.ToLower().Contains(search!)
                || member.Name.Last.ToLower().Contains(search!)
                || member.Email.Value.Contains(search!));

        var page = await members.ListAsync(options, cancellationToken);
        return page.Map(member => member.ToDto());
    }
}
