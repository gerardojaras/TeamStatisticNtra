using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TeamSearch.Infrastructure.Factories;

public class ToolingTeamSearchDbContextFactory : IDesignTimeDbContextFactory<TeamSearchDbContext>
{
    public TeamSearchDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<TeamSearchDbContext>();
        var connectionString = "Data Source=teamsearch.db";
        builder.UseSqlite(connectionString, sql => sql.MigrationsAssembly("TeamSearch.Infrastructure"));
        return new TeamSearchDbContext(builder.Options);
    }
}

