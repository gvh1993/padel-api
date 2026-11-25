using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Courts;

namespace Padel.Application.Features.Courts;

public sealed record SetCourtMaintenanceCommand(Guid Id, bool UnderMaintenance) : IRequest<CourtDto>;

public sealed class SetCourtMaintenanceCommandHandler(
    ICourtRepository courts,
    IUnitOfWork unitOfWork) : IRequestHandler<SetCourtMaintenanceCommand, CourtDto>
{
    public async Task<CourtDto> Handle(SetCourtMaintenanceCommand request, CancellationToken cancellationToken)
    {
        var court = await courts.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Court>(request.Id);

        court.SetMaintenance(request.UnderMaintenance);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return court.ToDto();
    }
}
