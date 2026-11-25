using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Padel.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261125165209_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

        migrationBuilder.CreateTable(
            name: "Courts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Surface = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Courts", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Members",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Phone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                SuspensionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                RegisteredOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                FirstName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                LastName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Members", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Bookings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                CourtId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                SlotEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                SlotStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Bookings", x => x.Id);
                table.ForeignKey(
                    name: "FK_Bookings_Courts_CourtId",
                    column: x => x.CourtId,
                    principalTable: "Courts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Bookings_Members_MemberId",
                    column: x => x.MemberId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Bookings_CourtId",
            table: "Bookings",
            column: "CourtId");

        migrationBuilder.CreateIndex(
            name: "IX_Bookings_MemberId",
            table: "Bookings",
            column: "MemberId");

        // Business-rule guards that EF Core cannot model. They close the race between
        // "check" and "insert" in the handlers. Names must match DatabaseConstraints.
        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX "IX_Members_Email" ON "Members" ("Email");
            """);

        migrationBuilder.Sql("""
            ALTER TABLE "Bookings" ADD CONSTRAINT "EX_Bookings_NoOverlappingSlots"
                EXCLUDE USING gist ("CourtId" WITH =, tstzrange("SlotStart", "SlotEnd") WITH &&)
                WHERE ("Status" <> 'Cancelled');
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Bookings");

        migrationBuilder.DropTable(
            name: "Courts");

        migrationBuilder.DropTable(
            name: "Members");

        migrationBuilder.AlterDatabase()
            .OldAnnotation("Npgsql:PostgresExtension:btree_gist", ",,");
    }
}
