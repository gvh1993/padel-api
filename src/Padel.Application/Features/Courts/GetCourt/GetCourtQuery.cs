using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Courts;

namespace Padel.Application.Features.Courts;

public sealed record GetCourtQuery(Guid Id) : IRequest<CourtDto>;

public sealed class GetCourtQueryHandler(ICourtRepository courts)
    : IRequestHandler<GetCourtQuery, CourtDto>
{
    public async Task<CourtDto> Handle(GetCourtQuery request, CancellationToken cancellationToken)
    {
        var court = await courts.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Court>(request.Id);

        return court.ToDto();
    }
}
