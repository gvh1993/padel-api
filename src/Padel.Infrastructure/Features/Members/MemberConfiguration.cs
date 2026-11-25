using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Padel.Domain.Common;
using Padel.Domain.Members;

namespace Padel.Infrastructure.Features.Members;

internal sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");
        builder.HasKey(member => member.Id);
        builder.Property(member => member.Id).ValueGeneratedNever();

        builder.ComplexProperty(member => member.Name, name =>
        {
            name.Property(n => n.First).HasColumnName("FirstName").HasMaxLength(FullName.MaxLength);
            name.Property(n => n.Last).HasColumnName("LastName").HasMaxLength(FullName.MaxLength);
        });

        builder.ComplexProperty(member => member.Email, email =>
            email.Property(e => e.Value).HasColumnName("Email").HasMaxLength(Email.MaxLength));

        builder.Property(member => member.Phone).HasMaxLength(Member.PhoneMaxDigits);
        builder.Property(member => member.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(member => member.SuspensionReason).HasMaxLength(Reason.MaxLength);
        builder.Property(member => member.RegisteredOn);

        // Maps to PostgreSQL's xmin system column: optimistic concurrency without an extra column.
        // PostgreSQL's xmin system column: changes on every update, checked on every save.
        builder.Property(member => member.Version).HasColumnName("xmin").IsRowVersion();

        builder.Ignore(member => member.DomainEvents);
    }
}
