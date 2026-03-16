using Microsoft.EntityFrameworkCore;
using TeamSearch.Domain;

namespace TeamSearch.Infrastructure;

public class TeamSearchDbContext(DbContextOptions<TeamSearchDbContext> options) : DbContext(options)
{
    public DbSet<TeamRecord> TeamRecords { get; set; } = null!;

    public string? CurrentUserId { get; set; }

    public IQueryable<TeamRecord> TeamRecordsWithDeleted()
    {
        return TeamRecords.IgnoreQueryFilters();
    }

    public Task<TeamRecord?> FindTeamRecordIncludingDeletedAsync(int id, CancellationToken cancellationToken = default)
    {
        return TeamRecords.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

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
            b.Property(t => t.CreatedAt).IsRequired();
            b.Property(t => t.CreatedBy).HasMaxLength(200);
            b.Property(t => t.LastModifiedAt);
            b.Property(t => t.LastModifiedBy).HasMaxLength(200);
            b.Property(t => t.IsDeleted).HasDefaultValue(false);
            b.Property(t => t.DeletedAt);
            b.Property(t => t.DeletedBy).HasMaxLength(200);
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

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
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

            switch (entry.State)
            {
                case EntityState.Added:
                {
                    entity.CreatedAt = now;
                    if (!string.IsNullOrEmpty(CurrentUserId))
                        entity.CreatedBy = CurrentUserId;

                    entity.IsDeleted = false;
                    break;
                }
                case EntityState.Modified:
                {
                    entity.LastModifiedAt = now;
                    if (!string.IsNullOrEmpty(CurrentUserId))
                        entity.LastModifiedBy = CurrentUserId;

                    var isDeletedProp = entry.Property(e => e.IsDeleted);
                    if (!isDeletedProp.IsModified || !entity.IsDeleted) continue;
                    entity.DeletedAt = now;
                    if (!string.IsNullOrEmpty(CurrentUserId))
                        entity.DeletedBy = CurrentUserId;
                    break;
                }
                case EntityState.Deleted:
                {
                    entry.State = EntityState.Modified;
                    entity.IsDeleted = true;
                    entity.DeletedAt = now;
                    if (!string.IsNullOrEmpty(CurrentUserId))
                        entity.DeletedBy = CurrentUserId;
                    break;
                }
                case EntityState.Detached:
                case EntityState.Unchanged:
                default:
                    // Ignore detached/unchanged entries and any unexpected states instead of throwing.
                    break;
            }
        }
    }
}