using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TeamSearch.Domain;

namespace TeamSearch.Infrastructure.Repositories;

public class TeamRecordRepository
{
    private readonly TeamSearchDbContext _db;

    public TeamRecordRepository(TeamSearchDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// Restores (un-deletes) a soft-deleted TeamRecord by id. Returns true if restored.
    /// </summary>
    public async Task<bool> RestoreAsync(int id, string? performedBy = null, CancellationToken cancellationToken = default)
    {
        var entity = await _db.FindTeamRecordIncludingDeletedAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity == null) return false;
        if (!entity.IsDeleted) return false; // already active

        // Set current user for auditing and perform the restore
        _db.CurrentUserId = performedBy;
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
        entity.LastModifiedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(performedBy)) entity.LastModifiedBy = performedBy;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Permanently removes (hard-delete) a TeamRecord from the database. Returns true if removed.
    /// </summary>
    public async Task<bool> PurgeAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.FindTeamRecordIncludingDeletedAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity == null) return false;

        _db.TeamRecords.Remove(entity);
        // Do not set CurrentUserId for hard delete (it is implementation choice)
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Permanently removes all soft-deleted records. Returns number of removed rows.
    /// </summary>
    public async Task<int> PurgeAllSoftDeletedAsync(CancellationToken cancellationToken = default)
    {
        var deleted = await _db.TeamRecordsWithDeleted().Where(t => t.IsDeleted).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (deleted == null || deleted.Count == 0) return 0;

        _db.TeamRecords.RemoveRange(deleted);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return deleted.Count;
    }
}

