using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Padel.Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c>. Creating migrations does not connect to the database.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PadelDbContext>
{
    public PadelDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__padel")
            ?? "Host=localhost;Port=5432;Database=padel;Username=padel;Password=padel";

        var options = new DbContextOptionsBuilder<PadelDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new PadelDbContext(options);
    }
}
