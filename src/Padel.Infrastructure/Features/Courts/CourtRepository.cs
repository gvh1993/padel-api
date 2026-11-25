using Padel.Application.Features.Courts;
using Padel.Domain.Courts;
using Padel.Infrastructure.Persistence;

namespace Padel.Infrastructure.Features.Courts;

public sealed class CourtRepository(PadelDbContext context)
    : EfRepository<Court>(context), ICourtRepository;
