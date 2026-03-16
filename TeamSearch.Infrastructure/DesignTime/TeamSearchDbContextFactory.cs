using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TeamSearch.Infrastructure;

public class TeamSearchDbContextFactory : IDesignTimeDbContextFactory<TeamSearchDbContext>
{
    public TeamSearchDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<TeamSearchDbContext>();

        // Use the same connection string we use at runtime by default.
        var connectionString = "Data Source=teamsearch.db";
        builder.UseSqlite(connectionString, sql => sql.MigrationsAssembly("TeamSearch.Infrastructure"));

        return new TeamSearchDbContext(builder.Options);
    }
}

