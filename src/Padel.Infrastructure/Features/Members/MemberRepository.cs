using Microsoft.EntityFrameworkCore;
using Padel.Application.Features.Members;
using Padel.Domain.Common;
using Padel.Domain.Members;
using Padel.Infrastructure.Persistence;

namespace Padel.Infrastructure.Features.Members;

public sealed class MemberRepository(PadelDbContext context)
    : EfRepository<Member>(context), IMemberRepository
{
    public Task<Member?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        var value = email.Value;
        return Set.FirstOrDefaultAsync(member => member.Email.Value == value, cancellationToken);
    }
}
