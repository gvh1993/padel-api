using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Padel.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class BookingRules : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Bookings_MemberId",
            table: "Bookings");

        migrationBuilder.AddColumn<string>(
            name: "IdempotencyKey",
            table: "Bookings",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Bookings_MemberId_IdempotencyKey",
            table: "Bookings",
            columns: new[] { "MemberId", "IdempotencyKey" },
            unique: true,
            filter: "\"IdempotencyKey\" IS NOT NULL");

        // A member cannot be on two courts at the same time (cancelled bookings do not count).
        migrationBuilder.Sql("""
            ALTER TABLE "Bookings" ADD CONSTRAINT "EX_Bookings_NoOverlappingMemberSlots"
                EXCLUDE USING gist ("MemberId" WITH =, tstzrange("SlotStart", "SlotEnd") WITH &&)
                WHERE ("Status" <> 'Cancelled');
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""ALTER TABLE "Bookings" DROP CONSTRAINT "EX_Bookings_NoOverlappingMemberSlots";""");

        migrationBuilder.DropIndex(
            name: "IX_Bookings_MemberId_IdempotencyKey",
            table: "Bookings");

        migrationBuilder.DropColumn(
            name: "IdempotencyKey",
            table: "Bookings");

        migrationBuilder.CreateIndex(
            name: "IX_Bookings_MemberId",
            table: "Bookings",
            column: "MemberId");
    }
}
