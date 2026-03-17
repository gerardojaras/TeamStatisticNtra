using System.Globalization;
using System.Diagnostics;
using CsvHelper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TeamSearch.Domain;
using TeamSearch.Infrastructure;
using TeamSearch.Infrastructure.Utilities;
using TeamSearch.Seeder;
using TeamSearch.Shared.Dtos;

var argList = args.ToList();
var dryRun = argList.Contains("--dry-run") || argList.Contains("-n");
var runMigrations = argList.Contains("--migrate");
var positionalList = argList.Where(a => !a.StartsWith("-")).ToList();
var csvPath = positionalList.ElementAtOrDefault(0) ?? "data/CollegeFootballTeamWinsWithMascots.csv";
var importerUserId = positionalList.ElementAtOrDefault(1) ?? "seeder";

var stopwatch = Stopwatch.StartNew();
void Log(string message) => Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] +{stopwatch.ElapsedMilliseconds}ms {message}");

var processed = 0;
var inserted = 0;
var updated = 0;

SqliteConnection? sqliteConnection = null;
TeamSearchDbContext? db = null;

var connectionString = SqliteConnectionStringResolver.Resolve("Data Source=teamsearch.db");

try
{
    Log("Seeder start");

    sqliteConnection = new SqliteConnection(connectionString);
    sqliteConnection.Open();
    using (var cmd = sqliteConnection.CreateCommand())
    {
        cmd.CommandText = "PRAGMA busy_timeout = 200;";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "PRAGMA journal_mode = WAL;";
        cmd.ExecuteNonQuery();
    }
    Log("SQLite connection open and PRAGMA configured");

    var optionsBuilder = new DbContextOptionsBuilder<TeamSearchDbContext>();
    optionsBuilder.UseSqlite(sqliteConnection, sql => sql.MigrationsAssembly("TeamSearch.Infrastructure"));
    db = new TeamSearchDbContext(optionsBuilder.Options);

    if (!dryRun && runMigrations)
    {
        Log("Migration start");
        await db.Database.MigrateAsync();
        Log("Migration complete");
    }
    else
    {
        Log("Migration skipped (pass --migrate to enable)");
    }

    using var reader = new StreamReader(csvPath);
    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
    csv.Context.RegisterClassMap<CsvTeamRecordMap>();
    var records = csv.GetRecords<CsvTeamRecord>();

    db.ChangeTracker.AutoDetectChangesEnabled = false;
    Log("CSV processing start");

    await foreach (var r in records.ToAsyncEnumerable())
    {
        processed++;
        if (processed % 25 == 0) Log($"Processed {processed} records");

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

    Log("SaveChanges start");
    db.CurrentUserId = importerUserId;
    await db.SaveChangesAsync();
    Log("SaveChanges complete");

    Environment.ExitCode = 0;
}
catch (Exception ex)
{
    Log($"ERROR: {ex.GetType().Name}: {ex.Message}");
    Environment.ExitCode = 1;
}
finally
{
    Console.WriteLine($"[SUMMARY] processed={processed}, inserted={inserted}, updated={updated}, dryRun={dryRun}");
    Log("Disposal start");

    if (db is not null) await db.DisposeAsync();
    if (sqliteConnection is not null)
    {
        sqliteConnection.Close();
        sqliteConnection.Dispose();
    }

    Log("Disposal complete; exiting");
    Environment.Exit(Environment.ExitCode);
}
