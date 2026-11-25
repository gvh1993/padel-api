using Padel.Application.Common.Messaging;
using Padel.Application.Common.Querying;
using Padel.Domain.Common;
using Padel.Domain.Courts;

namespace Padel.Application.Features.Courts;

public sealed record ListCourtsQuery : ListQuery, IRequest<PagedResult<CourtDto>>
{
    public CourtStatus? Status { get; init; }

    /// <summary>Case-insensitive exact match on the surface, e.g. <c>glass</c>.</summary>
    public string? Surface { get; init; }

    /// <summary>Case-insensitive match on the court name.</summary>
    public string? Search { get; init; }
}

public sealed class ListCourtsQueryHandler(ICourtRepository courts)
    : IRequestHandler<ListCourtsQuery, PagedResult<CourtDto>>
{
    public async Task<Result<PagedResult<CourtDto>>> Handle(ListCourtsQuery request, CancellationToken cancellationToken)
    {
        var status = request.Status;
        var surface = request.Surface?.Trim().ToLowerInvariant();
        var search = request.Search?.Trim().ToLowerInvariant();

        var options = ListOptions<Court>.From(request, CourtSorting.Map)
            .WhereIf(status.HasValue, court => court.Status == status)
            .WhereIf(!string.IsNullOrEmpty(surface), court => court.Surface.ToLower() == surface)
            .WhereIf(!string.IsNullOrEmpty(search), court => court.Name.ToLower().Contains(search!));

        var page = await courts.ListAsync(options, cancellationToken);
        return page.Map(court => court.ToDto());
    }
}
