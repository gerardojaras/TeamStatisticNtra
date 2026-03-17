using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeamSearch.Domain;
using TeamSearch.Infrastructure;
using TeamSearch.Infrastructure.Repositories;
using Xunit;

namespace TeamSearch.Tests;

public class TeamRecordRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public TeamRecordRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContextFactory<TeamSearchDbContext>(options => { options.UseSqlite(_connection); });

        _provider = services.BuildServiceProvider();

        // Ensure database schema is created
        var factory = _provider.GetRequiredService<IDbContextFactory<TeamSearchDbContext>>();
        using var db = factory.CreateDbContext();
        db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task ListAsync_ReturnsSeededRecords()
    {
        var factory = _provider.GetRequiredService<IDbContextFactory<TeamSearchDbContext>>();
        await using (var db = factory.CreateDbContext())
        {
            db.CurrentUserId = "tester";
            db.TeamRecords.Add(new TeamRecord { Team = "Alpha", Mascot = "A", Wins = 5 });
            db.TeamRecords.Add(new TeamRecord { Team = "Beta", Mascot = "B", Wins = 3 });
            await db.SaveChangesAsync();
        }

        var repo = new TeamRecordRepository(factory);
        var results = await repo.ListAsync();
        results.Should().HaveCountGreaterOrEqualTo(2);
        results.Any(r => r.Team == "Alpha").Should().BeTrue();
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        var factory = _provider.GetRequiredService<IDbContextFactory<TeamSearchDbContext>>();
        await using (var db = factory.CreateDbContext())
        {
            db.CurrentUserId = "tester";
            db.TeamRecords.Add(new TeamRecord { Team = "Gamma", Mascot = "G", Wins = 7 });
            await db.SaveChangesAsync();
        }

        var repo = new TeamRecordRepository(factory);
        var count = await repo.CountAsync();
        count.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task RestoreAsync_UndeletesSoftDeletedRecord()
    {
        var factory = _provider.GetRequiredService<IDbContextFactory<TeamSearchDbContext>>();
        int id;
        await using (var db = factory.CreateDbContext())
        {
            db.CurrentUserId = "tester";
            var rec = new TeamRecord { Team = "ToDelete", Mascot = "D", Wins = 0 };
            db.TeamRecords.Add(rec);
            await db.SaveChangesAsync();
            // Soft-delete via EF change tracking
            rec.IsDeleted = true;
            await db.SaveChangesAsync();
            id = rec.Id;
        }

        var repo = new TeamRecordRepository(factory);
        var restored = await repo.RestoreAsync(id, "restorer");
        restored.Should().BeTrue();

        await using (var db = factory.CreateDbContext())
        {
            var entity = await db.TeamRecords.FirstOrDefaultAsync(t => t.Id == id);
            entity.Should().NotBeNull();
            entity!.IsDeleted.Should().BeFalse();
            entity.LastModifiedBy.Should().Be("restorer");
        }
    }

    [Fact]
    public async Task PurgeAsync_RemovesRecordPermanently()
    {
        var factory = _provider.GetRequiredService<IDbContextFactory<TeamSearchDbContext>>();
        int id;
        await using (var db = factory.CreateDbContext())
        {
            db.CurrentUserId = "tester";
            var rec = new TeamRecord { Team = "ToPurge", Mascot = "P", Wins = 1 };
            db.TeamRecords.Add(rec);
            await db.SaveChangesAsync();
            id = rec.Id;
            // Soft-delete first
            rec.IsDeleted = true;
            await db.SaveChangesAsync();
        }

        var repo = new TeamRecordRepository(factory);
        var purged = await repo.PurgeAsync(id);
        purged.Should().BeTrue();

        await using (var db = factory.CreateDbContext())
        {
            var entity = await db.TeamRecords.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
            entity.Should().BeNull();
        }
    }
}