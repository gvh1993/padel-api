using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Padel.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class MemberIdentity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "IdentityId",
            table: "Members",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Members_IdentityId",
            table: "Members",
            column: "IdentityId",
            unique: true,
            filter: "\"IdentityId\" IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Members_IdentityId",
            table: "Members");

        migrationBuilder.DropColumn(
            name: "IdentityId",
            table: "Members");
    }
}
