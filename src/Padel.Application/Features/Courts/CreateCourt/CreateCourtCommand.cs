using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Common;
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
    public async Task<Result<CourtDto>> Handle(CreateCourtCommand request, CancellationToken cancellationToken)
    {
        var courtResult = Court.Create(request.Name, request.Surface);
        if (courtResult is not Court court)
        {
            return courtResult.Error;
        }

        courts.Add(court);
        return await unitOfWork.SaveChangesAsync(cancellationToken) is Error saveError ? saveError : court.ToDto();
    }
}
