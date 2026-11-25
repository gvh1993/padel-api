using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Courts;

namespace Padel.Application.Features.Courts;

public sealed record CreateCourtCommand : IRequest<CourtDto>
{
    public required string Name { get; init; }
    public required string Surface { get; init; }
}

public sealed class CreateCourtCommandHandler(
    ICourtRepository courts,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateCourtCommand, CourtDto>
{
    public async Task<CourtDto> Handle(CreateCourtCommand request, CancellationToken cancellationToken)
    {
        var court = new Court(request.Name, request.Surface);

        courts.Add(court);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return court.ToDto();
    }
}
