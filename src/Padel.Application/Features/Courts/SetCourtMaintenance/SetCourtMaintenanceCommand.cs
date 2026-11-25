using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Common;
using Padel.Domain.Courts;

namespace Padel.Application.Features.Courts;

public sealed record SetCourtMaintenanceCommand(Guid Id, bool UnderMaintenance) : IRequest<CourtDto>;

public sealed class SetCourtMaintenanceCommandHandler(
    ICourtRepository courts,
    IUnitOfWork unitOfWork) : IRequestHandler<SetCourtMaintenanceCommand, CourtDto>
{
    public async Task<Result<CourtDto>> Handle(SetCourtMaintenanceCommand request, CancellationToken cancellationToken)
    {
        var court = await courts.GetByIdAsync(request.Id, cancellationToken);
        if (court is null)
        {
            return ApplicationErrors.NotFound<Court>(request.Id);
        }

        if (court.SetMaintenance(request.UnderMaintenance) is Error changedError)
        {
            return changedError;
        }

        return await unitOfWork.SaveChangesAsync(cancellationToken) is Error saveError ? saveError : court.ToDto();
    }
}
