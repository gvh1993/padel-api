using Padel.Domain.Common;

namespace Padel.Domain.Courts;

public sealed record CourtCreated(Guid CourtId) : DomainEvent;

public sealed record CourtRetired(Guid CourtId) : DomainEvent;
