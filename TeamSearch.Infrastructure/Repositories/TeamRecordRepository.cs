using Microsoft.EntityFrameworkCore;
using TeamSearch.Application.Repositories;
using TeamSearch.Domain;

namespace TeamSearch.Infrastructure.Repositories;

public class TeamRecordRepository(IDbContextFactory<TeamSearchDbContext> factory) : ITeamRecordRepository
{
    private readonly IDbContextFactory<TeamSearchDbContext> _factory =
        factory ?? throw new ArgumentNullException(nameof(factory));

    public async Task<bool> RestoreAsync(int id, string? performedBy = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = _factory.CreateDbContext();
        var entity = await db.FindTeamRecordIncludingDeletedAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity == null) return false;
        if (!entity.IsDeleted) return false;

        db.CurrentUserId = performedBy;
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
        entity.LastModifiedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(performedBy)) entity.LastModifiedBy = performedBy;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> PurgeAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = _factory.CreateDbContext();
        var entity = await db.FindTeamRecordIncludingDeletedAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity == null) return false;

        db.TeamRecords.Remove(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<TeamRecord?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = _factory.CreateDbContext();
        return await db.TeamRecords.FirstOrDefaultAsync(t => t.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<TeamRecord>> ListAsync(string? q = null, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        await using var db = _factory.CreateDbContext();

        IQueryable<TeamRecord> query = db.TeamRecords;
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            // Team is non-nullable by domain model; only Mascot needs a null check
            query = query.Where(t => EF.Functions.Like(t.Team, $"%{term}%") ||
                                     (t.Mascot != null && EF.Functions.Like(t.Mascot, $"%{term}%")));
        }

        return await query.OrderBy(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CountAsync(string? q = null, CancellationToken cancellationToken = default)
    {
        await using var db = _factory.CreateDbContext();
        IQueryable<TeamRecord> query = db.TeamRecords;
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(t => EF.Functions.Like(t.Team, $"%{term}%") ||
                                     (t.Mascot != null && EF.Functions.Like(t.Mascot, $"%{term}%")));
        }

        return await query.CountAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> PurgeAllSoftDeletedAsync(CancellationToken cancellationToken = default)
    {
        await using var db = _factory.CreateDbContext();
        var deleted = await db.TeamRecordsWithDeleted().Where(t => t.IsDeleted).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (deleted.Count == 0) return 0;

        db.TeamRecords.RemoveRange(deleted);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return deleted.Count;
    }
}