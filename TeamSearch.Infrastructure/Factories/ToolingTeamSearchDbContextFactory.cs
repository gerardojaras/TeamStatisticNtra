// Official design-time DbContext factory for EF Core tooling.
//
// This class is intentionally the single, authoritative implementation
// of IDesignTimeDbContextFactory<TeamSearchDbContext> in the project so
// that `dotnet ef` and other EF tooling have a stable, explicit way to
// create a TeamSearchDbContext at design time. Do not remove unless you
// replace it with an alternative tooling factory.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TeamSearch.Infrastructure.Utilities;

namespace TeamSearch.Infrastructure.Factories;

public class ToolingTeamSearchDbContextFactory : IDesignTimeDbContextFactory<TeamSearchDbContext>
{
    public TeamSearchDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<TeamSearchDbContext>();
        var connectionString = "Data Source=teamsearch.db";
        // Resolve to canonical location under TeamSearch.Server
        connectionString = SqliteConnectionStringResolver.Resolve(connectionString);
        builder.UseSqlite(connectionString, sql => sql.MigrationsAssembly("TeamSearch.Infrastructure"));
        return new TeamSearchDbContext(builder.Options);
    }
}