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

        builder.HasOne<Member>().WithMany().HasForeignKey(booking => booking.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Court>().WithMany().HasForeignKey(booking => booking.CourtId).OnDelete(DeleteBehavior.Restrict);

        builder.Property<uint>(DatabaseConstraints.RowVersionColumn).IsRowVersion();

        builder.Ignore(booking => booking.DomainEvents);

        // The exclusion constraint that prevents overlapping bookings is created in the
        // InitialCreate migration (EF Core cannot model exclusion constraints).
    }
}
