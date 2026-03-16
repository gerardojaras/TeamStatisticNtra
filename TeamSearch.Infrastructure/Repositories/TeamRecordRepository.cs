using Microsoft.EntityFrameworkCore;
using TeamSearch.Application.Repositories;
using TeamSearch.Domain;

namespace TeamSearch.Infrastructure.Repositories;

public class TeamRecordRepository(TeamSearchDbContext db) : ITeamRecordRepository
{
    private readonly TeamSearchDbContext _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<bool> RestoreAsync(int id, string? performedBy = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.FindTeamRecordIncludingDeletedAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity == null) return false;
        if (!entity.IsDeleted) return false;

        _db.CurrentUserId = performedBy;
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
        entity.LastModifiedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(performedBy)) entity.LastModifiedBy = performedBy;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> PurgeAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.FindTeamRecordIncludingDeletedAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity == null) return false;

        _db.TeamRecords.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public Task<TeamRecord?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        return _db.TeamRecords.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public Task<List<TeamRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        return _db.TeamRecords.ToListAsync(cancellationToken);
    }

    public async Task<int> PurgeAllSoftDeletedAsync(CancellationToken cancellationToken = default)
    {
        var deleted = await _db.TeamRecordsWithDeleted().Where(t => t.IsDeleted).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (deleted.Count == 0) return 0;

        _db.TeamRecords.RemoveRange(deleted);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return deleted.Count;
    }
}