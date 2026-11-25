using Padel.Application.Common;
using Padel.Application.Common.Messaging;
using Padel.Domain.Courts;

namespace Padel.Application.Features.Courts;

public sealed record RenameCourtCommand(Guid Id, string Name) : IRequest<CourtDto>;

public sealed class RenameCourtCommandHandler(
    ICourtRepository courts,
    IUnitOfWork unitOfWork) : IRequestHandler<RenameCourtCommand, CourtDto>
{
    public async Task<CourtDto> Handle(RenameCourtCommand request, CancellationToken cancellationToken)
    {
        var court = await courts.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<Court>(request.Id);

        court.Rename(request.Name);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return court.ToDto();
    }
}
