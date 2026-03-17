using System.Text.RegularExpressions;
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

        // Perform a direct SQL delete to bypass the DbContext audit rules that
        // convert deletes into soft-deletes. Use parameterized SQL to avoid injection.
        var sql = "DELETE FROM \"TeamRecords\" WHERE \"Id\" = @p0;";
        var rows = await db.Database.ExecuteSqlRawAsync(sql, new object[] { id }, cancellationToken)
            .ConfigureAwait(false);
        return rows > 0;
    }

    public async Task<TeamRecord?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.TeamRecords.FirstOrDefaultAsync(t => t.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<TeamRecord>> ListAsync(string? q = null, int page = 1, int pageSize = 20,
        string? sortBy = null, string sortDir = "asc", IEnumerable<string>? fields = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        IQueryable<TeamRecord> query = db.TeamRecords;
        if (!string.IsNullOrWhiteSpace(q))
        {
            // Collapse multiple whitespace between terms and split into tokens
            var collapsed = Regex.Replace(q.Trim(), "\\s+", " ");
            var tokens = collapsed.Split(' ').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();

            // Determine which fields to search. Default to Team and Mascot if none provided.
            var fieldList = fields == null || !fields.Any()
                ? new[] { "team", "mascot" }
                : fields.Select(f => f.Trim().ToLowerInvariant()).Where(f => !string.IsNullOrEmpty(f)).Distinct()
                    .ToArray();

            // Separate exclusion (negation) tokens (prefix '-' or '!') and inclusion tokens.
            var exclusionTokens = tokens.Where(t => t.StartsWith('-') || t.StartsWith('!'))
                .Select(t => t.Substring(1)).Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Replace(" ", string.Empty).ToLowerInvariant()).ToList();

            var inclusionTokens = tokens.Where(t => !(t.StartsWith('-') || t.StartsWith('!')))
                .Select(t => t.Replace(" ", string.Empty).ToLowerInvariant()).ToList();

            // Apply exclusions first: remove any record that matches any exclusion token in any requested field.
            if (exclusionTokens.Count > 0)
                foreach (var ex in exclusionTokens)
                {
                    var token = ex; // local copy for closure
                    query = query.Where(t => !(
                        (fieldList.Contains("team") && t.Team != null &&
                         t.Team.Replace(" ", string.Empty).ToLower().Contains(token)) ||
                        (fieldList.Contains("mascot") && t.Mascot != null &&
                         t.Mascot.Replace(" ", string.Empty).ToLower().Contains(token)) ||
                        (fieldList.Contains("createdby") && t.CreatedBy != null &&
                         t.CreatedBy.Replace(" ", string.Empty).ToLower().Contains(token))
                    ));
                }

            // Apply inclusions: require that the record matches each inclusion token (AND across tokens).
            if (inclusionTokens.Count > 0)
                foreach (var inc in inclusionTokens)
                {
                    var token = inc; // local copy for closure
                    query = query.Where(t =>
                        (fieldList.Contains("team") && t.Team != null &&
                         t.Team.Replace(" ", string.Empty).ToLower().Contains(token)) ||
                        (fieldList.Contains("mascot") && t.Mascot != null &&
                         t.Mascot.Replace(" ", string.Empty).ToLower().Contains(token)) ||
                        (fieldList.Contains("createdby") && t.CreatedBy != null &&
                         t.CreatedBy.Replace(" ", string.Empty).ToLower().Contains(token)));
                }
        }

        // Apply ordering based on allowed fields. Default to Id ascending.
        sortDir = (sortDir ?? "asc").ToLowerInvariant() == "desc" ? "desc" : "asc";
        query = sortBy?.ToLowerInvariant() switch
        {
            "team" => sortDir == "desc" ? query.OrderByDescending(t => t.Team) : query.OrderBy(t => t.Team),
            "mascot" => sortDir == "desc" ? query.OrderByDescending(t => t.Mascot) : query.OrderBy(t => t.Mascot),
            "rank" => sortDir == "desc" ? query.OrderByDescending(t => t.Rank) : query.OrderBy(t => t.Rank),
            "wins" => sortDir == "desc" ? query.OrderByDescending(t => t.Wins) : query.OrderBy(t => t.Wins),
            "winningpercentage" => sortDir == "desc"
                ? query.OrderByDescending(t => t.WinningPercentage)
                : query.OrderBy(t => t.WinningPercentage),
            "createdat" => sortDir == "desc"
                ? query.OrderByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.CreatedAt),
            _ => sortDir == "desc" ? query.OrderByDescending(t => t.Id) : query.OrderBy(t => t.Id)
        };

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CountAsync(string? q = null, IEnumerable<string>? fields = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        IQueryable<TeamRecord> query = db.TeamRecords;
        if (string.IsNullOrWhiteSpace(q)) return await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var normalized = Regex.Replace(q.Trim(), "\\s+", " ")
            .Replace(" ", string.Empty)
            .ToLowerInvariant();

        var fieldList = fields == null || !fields.Any()
            ? new[] { "team", "mascot" }
            : fields.Select(f => f.Trim().ToLowerInvariant()).Where(f => !string.IsNullOrEmpty(f)).Distinct()
                .ToArray();

        query = query.Where(t =>
            (fieldList.Contains("team") &&
             t.Team.Replace(" ", string.Empty).ToLower().Contains(normalized)) ||
            (fieldList.Contains("mascot") && t.Mascot != null &&
             t.Mascot.Replace(" ", string.Empty).ToLower().Contains(normalized)) ||
            (fieldList.Contains("createdby") && t.CreatedBy != null &&
             t.CreatedBy.Replace(" ", string.Empty).ToLower().Contains(normalized)));

        return await query.CountAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> PurgeAllSoftDeletedAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        // Use a direct SQL DELETE to permanently remove soft-deleted rows.
        var rows = await db.Database
            .ExecuteSqlRawAsync("DELETE FROM \"TeamRecords\" WHERE \"IsDeleted\" = 1;", cancellationToken)
            .ConfigureAwait(false);
        return rows;
    }
}