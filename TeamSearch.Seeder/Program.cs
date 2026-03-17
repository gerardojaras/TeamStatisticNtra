using System.Globalization;
using CsvHelper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TeamSearch.Domain;
using TeamSearch.Infrastructure;
using TeamSearch.Seeder;
using TeamSearch.Shared.Dtos;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging((_, logging) => { logging.ClearProviders(); logging.AddFilter("Microsoft", LogLevel.Warning); logging.AddFilter("System", LogLevel.Warning); })
    .ConfigureServices((ctx, services) =>
    {
        var connectionString = ctx.Configuration.GetConnectionString("Default") ?? "Data Source=teamsearch.db";
        connectionString = TeamSearch.Infrastructure.Utilities.SqliteConnectionStringResolver.Resolve(connectionString);

        var sqliteConnection = new SqliteConnection(connectionString);
        sqliteConnection.Open();
        using var cmd = sqliteConnection.CreateCommand();
        cmd.CommandText = "PRAGMA busy_timeout = 200;";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "PRAGMA journal_mode = WAL;";
        cmd.ExecuteNonQuery();

        services.AddSingleton(sqliteConnection);
        services.AddDbContextFactory<TeamSearchDbContext>(options => options.UseSqlite(sqliteConnection, sql => sql.MigrationsAssembly("TeamSearch.Infrastructure")));
    })
    .Build();

try
{
    using var scope = host.Services.CreateScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TeamSearchDbContext>>();
    await using var db = dbFactory.CreateDbContext();

    var argList = args.ToList();
    var dryRun = argList.Contains("--dry-run") || argList.Contains("-n");
    var positionalList = argList.Where(a => !a.StartsWith("-")).ToList();
    var csvPath = positionalList.ElementAtOrDefault(0) ?? "data/CollegeFootballTeamWinsWithMascots.csv";
    var importerUserId = positionalList.ElementAtOrDefault(1) ?? "seeder";

    if (!dryRun) await db.Database.MigrateAsync();

    var processed = 0;
    var inserted = 0;
    var updated = 0;

    using var reader = new StreamReader(csvPath);
    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
    csv.Context.RegisterClassMap<CsvTeamRecordMap>();
    var records = csv.GetRecords<CsvTeamRecord>();

    var autoDetect = db.ChangeTracker.AutoDetectChangesEnabled;
    db.ChangeTracker.AutoDetectChangesEnabled = false;

    await foreach (var r in records.ToAsyncEnumerable())
    {
        processed++;
        if (dryRun)
        {
            inserted++;
            continue;
        }

        var existing = await db.TeamRecordsWithDeleted().FirstOrDefaultAsync(t => t.Team == r.Team);
        if (existing != null)
        {
            existing.Rank = r.Rank;
            existing.Mascot = r.Mascot;
            existing.DateOfLastWin = r.DateOfLastWin;
            existing.WinningPercentage = r.WinningPercentage;
            existing.Wins = r.Wins;
            existing.Losses = r.Losses;
            existing.Ties = r.Ties;
            existing.Games = r.Games;
            existing.LastModifiedAt = DateTime.UtcNow;
            existing.LastModifiedBy = importerUserId;
            updated++;
        }
        else
        {
            var entity = new TeamRecord
            {
                Team = r.Team,
                Rank = r.Rank,
                Mascot = r.Mascot,
                DateOfLastWin = r.DateOfLastWin,
                WinningPercentage = r.WinningPercentage,
                Wins = r.Wins,
                Losses = r.Losses,
                Ties = r.Ties,
                Games = r.Games,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = importerUserId
            };
            db.TeamRecords.Add(entity);
            inserted++;
        }
    }

    db.CurrentUserId = importerUserId;
    await db.SaveChangesAsync();
    db.ChangeTracker.Clear();
    db.ChangeTracker.AutoDetectChangesEnabled = autoDetect;

    if (dryRun) Console.WriteLine($"Dry run summary: processed={processed}, inserted={inserted}, updated={updated}");
    Environment.ExitCode = 0;
}
catch
{
    Environment.ExitCode = 1;
}
finally
{
    try { host.Dispose(); } catch { }
    Environment.Exit(Environment.ExitCode);
}

