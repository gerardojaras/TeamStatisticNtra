using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace TeamSearch.Infrastructure.Factories;

public class TeamSearchDbContextProvider(IConfiguration config)
{
    private readonly IConfiguration _config = config ?? throw new ArgumentNullException(nameof(config));

    public TeamSearchDbContext CreateDbContext()
    {
        var connectionString = _config.GetConnectionString("Default") ?? "Data Source=teamsearch.db";
        var optionsBuilder = new DbContextOptionsBuilder<TeamSearchDbContext>();
        optionsBuilder.UseSqlite(connectionString, sql => sql.MigrationsAssembly("TeamSearch.Infrastructure"));
        return new TeamSearchDbContext(optionsBuilder.Options);
    }
}