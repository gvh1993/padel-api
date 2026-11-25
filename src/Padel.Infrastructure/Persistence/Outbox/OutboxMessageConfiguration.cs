using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Padel.Infrastructure.Persistence.Outbox;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();
        builder.Property(message => message.Type).HasMaxLength(200);
        builder.Property(message => message.Payload).HasColumnType("jsonb");
        builder.Property(message => message.LastError).HasMaxLength(2000);

        // The processor only looks at messages that still have to be handled, oldest first.
        builder.HasIndex(message => message.OccurredOn)
            .HasDatabaseName("IX_OutboxMessages_Unprocessed")
            .HasFilter("\"ProcessedOn\" IS NULL");
    }
}
