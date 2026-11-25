using Padel.Application.Common;
using Padel.Domain.Common;
using Padel.Domain.Members;

namespace Padel.Application.Features.Members;

public interface IMemberRepository : IRepository<Member>
{
    Task<Member?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    Task<Member?> GetByIdentityIdAsync(string identityId, CancellationToken cancellationToken = default);
}
