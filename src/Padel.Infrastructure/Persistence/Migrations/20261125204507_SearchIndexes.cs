using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Padel.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class SearchIndexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:PostgresExtension:btree_gist", ",,")
            .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
            .OldAnnotation("Npgsql:PostgresExtension:btree_gist", ",,");

        // The searches lower-case the column and match anywhere in it: LIKE '%...%'.
        migrationBuilder.Sql("""
            CREATE INDEX "IX_Members_FirstName_Trigram" ON "Members" USING gin (lower("FirstName") gin_trgm_ops);
            CREATE INDEX "IX_Members_LastName_Trigram" ON "Members" USING gin (lower("LastName") gin_trgm_ops);
            CREATE INDEX "IX_Members_Email_Trigram" ON "Members" USING gin ("Email" gin_trgm_ops);
            CREATE INDEX "IX_Courts_Name_Trigram" ON "Courts" USING gin (lower("Name") gin_trgm_ops);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX "IX_Members_FirstName_Trigram";
            DROP INDEX "IX_Members_LastName_Trigram";
            DROP INDEX "IX_Members_Email_Trigram";
            DROP INDEX "IX_Courts_Name_Trigram";
            """);

        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:PostgresExtension:btree_gist", ",,")
            .OldAnnotation("Npgsql:PostgresExtension:btree_gist", ",,")
            .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
    }
}
