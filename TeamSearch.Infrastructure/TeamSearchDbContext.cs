using Microsoft.EntityFrameworkCore;
using TeamSearch.Domain;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TeamSearch.Infrastructure;

public class TeamSearchDbContext : DbContext
{
    public TeamSearchDbContext(DbContextOptions<TeamSearchDbContext> options)
        : base(options)
    {
    }

    public DbSet<TeamRecord> TeamRecords { get; set; } = null!;

    /// <summary>
    /// Returns TeamRecords including those that have been soft-deleted (ignores the global query filter).
    /// Use this when you need to access deleted rows for admin/restore scenarios.
    /// </summary>
    public IQueryable<TeamRecord> TeamRecordsWithDeleted()
        => TeamRecords.IgnoreQueryFilters();

    /// <summary>
    /// Convenience finder to get a TeamRecord by id including soft-deleted records.
    /// </summary>
    public Task<TeamRecord?> FindTeamRecordIncludingDeletedAsync(int id, CancellationToken cancellationToken = default)
        => TeamRecords.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    /// <summary>
    /// Optional id of the current user/process executing SaveChanges. Set this from the
    /// request scope (e.g. controller or middleware) before calling SaveChanges so auditing
    /// fields (CreatedBy/LastModifiedBy/DeletedBy) can be populated.
    /// </summary>
    public string? CurrentUserId { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TeamRecord>(b =>
        {
            b.ToTable("TeamRecords");
            b.HasKey(t => t.Id);

            b.Property(t => t.Team).IsRequired().HasMaxLength(200);
            b.Property(t => t.Mascot).HasMaxLength(100);

            b.Property(t => t.DateOfLastWin);
            b.Property(t => t.WinningPercentage);
            b.Property(t => t.Wins);
            b.Property(t => t.Losses);
            b.Property(t => t.Ties);
            b.Property(t => t.Games);

            // Auditing
            b.Property(t => t.CreatedAt).IsRequired();
            b.Property(t => t.CreatedBy).HasMaxLength(200);
            b.Property(t => t.LastModifiedAt);
            b.Property(t => t.LastModifiedBy).HasMaxLength(200);
            b.Property(t => t.IsDeleted).HasDefaultValue(false);
            b.Property(t => t.DeletedAt);
            b.Property(t => t.DeletedBy).HasMaxLength(200);

            // Soft-delete global filter
            b.HasQueryFilter(t => !t.IsDeleted);
        });
    }

    public override int SaveChanges()
    {
        ApplyAuditRules();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditRules();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditRules()
    {
        var entries = ChangeTracker.Entries<TeamRecord>().ToList();

        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            var entity = entry.Entity;

            if (entry.State == EntityState.Added)
            {
                // New entity: set created date/user and ensure not deleted
                entity.CreatedAt = now;
                if (!string.IsNullOrEmpty(CurrentUserId))
                    entity.CreatedBy = CurrentUserId;

                // Ensure defaults
                entity.IsDeleted = false;
            }
            else if (entry.State == EntityState.Modified)
            {
                // Modified: update last modified
                entity.LastModifiedAt = now;
                if (!string.IsNullOrEmpty(CurrentUserId))
                    entity.LastModifiedBy = CurrentUserId;

                // If IsDeleted was changed to true, populate deleted metadata
                var isDeletedProp = entry.Property(e => e.IsDeleted);
                if (isDeletedProp != null && isDeletedProp.IsModified && entity.IsDeleted)
                {
                    entity.DeletedAt = now;
                    if (!string.IsNullOrEmpty(CurrentUserId))
                        entity.DeletedBy = CurrentUserId;
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                // Soft-delete: convert to Modified, set IsDeleted and deleted metadata
                entry.State = EntityState.Modified;
                entity.IsDeleted = true;
                entity.DeletedAt = now;
                if (!string.IsNullOrEmpty(CurrentUserId))
                    entity.DeletedBy = CurrentUserId;
            }
        }
    }
}
