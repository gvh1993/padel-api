using Microsoft.EntityFrameworkCore;
using Padel.Domain.Bookings;
using Padel.Domain.Courts;
using Padel.Domain.Members;

namespace Padel.Infrastructure.Persistence;

public sealed class PadelDbContext(DbContextOptions<PadelDbContext> options) : DbContext(options)
{
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Needed for the booking exclusion constraint (equality on CourtId inside a GiST index).
        modelBuilder.HasPostgresExtension("btree_gist");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PadelDbContext).Assembly);
    }
}
