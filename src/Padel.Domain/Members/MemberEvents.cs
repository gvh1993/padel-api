using Padel.Domain.Common;

namespace Padel.Domain.Members;

public sealed record MemberRegistered(Guid MemberId) : DomainEvent;

public sealed record MemberSuspended(Guid MemberId, string? Reason) : DomainEvent;

public sealed record MemberReactivated(Guid MemberId) : DomainEvent;

public sealed record MemberIdentityLinked(Guid MemberId, string IdentityId) : DomainEvent;
