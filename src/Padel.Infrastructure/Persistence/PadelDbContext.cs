using Microsoft.EntityFrameworkCore;
using Padel.Domain.Bookings;
using Padel.Domain.Courts;
using Padel.Domain.Members;
using Padel.Infrastructure.Persistence.Outbox;

namespace Padel.Infrastructure.Persistence;

public sealed class PadelDbContext(DbContextOptions<PadelDbContext> options) : DbContext(options)
{
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Needed for the booking exclusion constraint (equality on CourtId inside a GiST index).
        modelBuilder.HasPostgresExtension("btree_gist");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PadelDbContext).Assembly);
    }
}
