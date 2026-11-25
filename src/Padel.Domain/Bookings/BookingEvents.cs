using Padel.Domain.Common;

namespace Padel.Domain.Bookings;

public sealed record BookingCreated(Guid BookingId, Guid MemberId, Guid CourtId) : DomainEvent;

public sealed record BookingCancelled(Guid BookingId, string? Reason) : DomainEvent;

public sealed record BookingCompleted(Guid BookingId) : DomainEvent;

public sealed record BookingNoShow(Guid BookingId) : DomainEvent;
