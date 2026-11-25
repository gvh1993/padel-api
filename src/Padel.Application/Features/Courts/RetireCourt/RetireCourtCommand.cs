using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Common;
using Padel.Domain.Courts;

namespace Padel.Application.Features.Courts;

public sealed record RetireCourtCommand(Guid Id) : IRequest<CourtDto>;

public sealed class RetireCourtCommandHandler(
    ICourtRepository courts,
    IUnitOfWork unitOfWork) : IRequestHandler<RetireCourtCommand, CourtDto>
{
    public async Task<Result<CourtDto>> Handle(RetireCourtCommand request, CancellationToken cancellationToken)
    {
        var court = await courts.GetByIdAsync(request.Id, cancellationToken);
        if (court is null)
        {
            return ApplicationErrors.NotFound<Court>(request.Id);
        }

        court.Retire();

        return await unitOfWork.SaveChangesAsync(cancellationToken) is Error saveError ? saveError : court.ToDto();
    }
}
