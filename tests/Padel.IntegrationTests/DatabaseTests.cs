using AwesomeAssertions;
using Npgsql;
using Padel.IntegrationTests.Infrastructure;

namespace Padel.IntegrationTests;

/// <summary>Checks on the schema the migrations create.</summary>
[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class DatabaseTests(PadelApiFactory factory)
{
    /// <summary>
    /// The searches run <c>lower(column) LIKE '%text%'</c>; only a trigram (GIN, gin_trgm_ops) index on
    /// exactly that expression can serve them.
    /// </summary>
    [Test]
    [Arguments("IX_Members_FirstName_Trigram", "lower((\"FirstName\")::text) gin_trgm_ops")]
    [Arguments("IX_Members_LastName_Trigram", "lower((\"LastName\")::text) gin_trgm_ops")]
    [Arguments("IX_Members_Email_Trigram", "\"Email\" gin_trgm_ops")]
    [Arguments("IX_Courts_Name_Trigram", "lower((\"Name\")::text) gin_trgm_ops")]
    public async Task SearchColumns_Should_HaveATrigramIndexOnTheSearchedExpression(string index, string expression)
    {
        await using var connection = new NpgsqlConnection(factory.DatabaseConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT indexdef FROM pg_indexes WHERE indexname = @index", connection);
        command.Parameters.AddWithValue("index", index);

        var definition = (string?)await command.ExecuteScalarAsync();

        definition.Should().NotBeNull().And.Contain("USING gin").And.Contain(expression);
    }
}
