using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Common;
using Padel.Domain.Courts;

namespace Padel.Application.Features.Courts;

public sealed record RenameCourtCommand(Guid Id, string Name) : IRequest<CourtDto>;

public sealed class RenameCourtCommandHandler(
    ICourtRepository courts,
    IUnitOfWork unitOfWork) : IRequestHandler<RenameCourtCommand, CourtDto>
{
    public async Task<Result<CourtDto>> Handle(RenameCourtCommand request, CancellationToken cancellationToken)
    {
        var court = await courts.GetByIdAsync(request.Id, cancellationToken);
        if (court is null)
        {
            return ApplicationErrors.NotFound<Court>(request.Id);
        }

        if (court.Rename(request.Name) is Error renamedError)
        {
            return renamedError;
        }

        return await unitOfWork.SaveChangesAsync(cancellationToken) is Error saveError ? saveError : court.ToDto();
    }
}
