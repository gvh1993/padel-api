using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Padel.Domain.Courts;
using Padel.Infrastructure.Persistence;

namespace Padel.Infrastructure.Features.Courts;

internal sealed class CourtConfiguration : IEntityTypeConfiguration<Court>
{
    public void Configure(EntityTypeBuilder<Court> builder)
    {
        builder.ToTable("Courts");
        builder.HasKey(court => court.Id);
        builder.Property(court => court.Id).ValueGeneratedNever();

        builder.Property(court => court.Name).HasMaxLength(Court.MaxLength);
        builder.Property(court => court.Surface).HasMaxLength(Court.MaxLength);
        builder.Property(court => court.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property<uint>(DatabaseConstraints.RowVersionColumn).IsRowVersion();

        builder.Ignore(court => court.DomainEvents);
    }
}
