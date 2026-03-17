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
    .ConfigureLogging((_, logging) =>
    {
        // Reduce log noise so seeder runs silently for normal operation
        logging.ClearProviders();
        logging.AddFilter("Microsoft", LogLevel.Warning);
        logging.AddFilter("System", LogLevel.Warning);
    })
    .ConfigureServices((ctx, services) =>
    {
        var connectionString = ctx.Configuration.GetConnectionString("Default") ?? "Data Source=teamsearch.db";
        connectionString = TeamSearch.Infrastructure.Utilities.SqliteConnectionStringResolver.Resolve(connectionString);

        // Create and open a shared SqliteConnection so we can configure PRAGMA settings
        // (busy_timeout) before EF Core runs migrations. Register the connection as a
        // singleton so it stays open for the lifetime of the host.
        var sqliteConnection = new SqliteConnection(connectionString);
        sqliteConnection.Open();
        using (var cmd = sqliteConnection.CreateCommand())
        {
            // Set busy timeout to 200ms so SQLite waits a little instead of failing quickly.
            cmd.CommandText = "PRAGMA busy_timeout = 200;";
            cmd.ExecuteNonQuery();
            // Use WAL journal mode to improve concurrent read/write behavior.
            cmd.CommandText = "PRAGMA journal_mode = WAL;";
            cmd.ExecuteNonQuery();
        }

        services.AddSingleton(sqliteConnection);
        // Register an IDbContextFactory so the seeder can create short-lived
        // TeamSearchDbContext instances while still using the shared SqliteConnection
        services.AddDbContextFactory<TeamSearchDbContext>(options =>
            options.UseSqlite(sqliteConnection, sql => sql.MigrationsAssembly("TeamSearch.Infrastructure")));
    })
    .Build();

// Start the host so hosted services, logging and the DI container are properly initialized.
// This ensures StopAsync/Dispose will clean up resources and the process can exit.
await host.StartAsync();

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
        }
        else
        {
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
    }

    // Save all changes once at the end (small dataset)
    db.CurrentUserId = importerUserId;
    await db.SaveChangesAsync();
    db.ChangeTracker.Clear();

    db.ChangeTracker.AutoDetectChangesEnabled = autoDetect;

    // Print a brief summary when running in dry-run mode so users can see simulated results.
    if (dryRun)
    {
        Console.WriteLine($"Dry run summary: processed={processed}, inserted={inserted}, updated={updated}");
    }
    // Silent exit on success for non-dry-run runs
    Environment.ExitCode = 0;
}
catch
{
    // Fail silently (non-zero exit code) — no console output
    Environment.ExitCode = 1;
}
finally
{
    try
    {
        // Attempt a graceful stop but don't wait forever — use a small timeout.
        var stopTask = host.StopAsync();
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
        var completed = await Task.WhenAny(stopTask, timeoutTask);
        if (completed != stopTask)
        {
            // StopAsync timed out; force dispose and exit to avoid hanging the process.
            host.Dispose();
            Environment.Exit(Environment.ExitCode);
        }
        // If StopAsync completed, ensure any exceptions are observed.
        await stopTask;
    }
    catch (Exception ex)
    {
        // Log to debug so any exception is observable without printing to standard output.
        System.Diagnostics.Debug.WriteLine($"Exception while stopping host: {ex}");
        try { host.Dispose(); } catch (Exception dex) { System.Diagnostics.Debug.WriteLine($"Dispose failed: {dex}"); }
        Environment.Exit(Environment.ExitCode);
    }
    finally
    {
        try { host.Dispose(); } catch (Exception dex) { System.Diagnostics.Debug.WriteLine($"Dispose failed in finally: {dex}"); }
        Environment.Exit(Environment.ExitCode);
    }
}

