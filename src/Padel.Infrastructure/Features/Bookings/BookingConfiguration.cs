using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Padel.Domain.Bookings;
using Padel.Domain.Common;
using Padel.Domain.Courts;
using Padel.Domain.Members;
using Padel.Infrastructure.Persistence;

namespace Padel.Infrastructure.Features.Bookings;

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");
        builder.HasKey(booking => booking.Id);
        builder.Property(booking => booking.Id).ValueGeneratedNever();

        builder.ComplexProperty(booking => booking.Slot, slot =>
        {
            slot.Property(s => s.Start).HasColumnName("SlotStart");
            slot.Property(s => s.End).HasColumnName("SlotEnd");
        });

        builder.Property(booking => booking.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(booking => booking.CancellationReason).HasMaxLength(Reason.MaxLength);
        builder.Property(booking => booking.CreatedOn);
        builder.Property(booking => booking.IdempotencyKey).HasMaxLength(100);

        // A key identifies one booking request per member.
        builder.HasIndex(booking => new { booking.MemberId, booking.IdempotencyKey })
            .HasDatabaseName(DatabaseConstraints.BookingIdempotencyKeyUnique)
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");

        builder.HasOne<Member>().WithMany().HasForeignKey(booking => booking.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Court>().WithMany().HasForeignKey(booking => booking.CourtId).OnDelete(DeleteBehavior.Restrict);

        // PostgreSQL's xmin system column: changes on every update, checked on every save.
        builder.Property(booking => booking.Version).HasColumnName("xmin").IsRowVersion();

        builder.Ignore(booking => booking.DomainEvents);

        // The exclusion constraints that prevent overlapping bookings (per court and per member) are
        // created in the migrations with SQL, because EF Core cannot model exclusion constraints.
    }
}
