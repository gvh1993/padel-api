using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Courts;

namespace Padel.Application.Features.Courts;

public sealed record RetireCourtCommand(Guid Id) : IRequest<CourtDto>;

public sealed class RetireCourtCommandHandler(
    ICourtRepository courts,
    IUnitOfWork unitOfWork) : IRequestHandler<RetireCourtCommand, CourtDto>
{
    public async Task<CourtDto> Handle(RetireCourtCommand request, CancellationToken cancellationToken)
    {
        var court = await courts.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Court>(request.Id);

        court.Retire();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return court.ToDto();
    }
}
