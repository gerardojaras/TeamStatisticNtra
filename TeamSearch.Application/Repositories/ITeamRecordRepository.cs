using TeamSearch.Domain;

namespace TeamSearch.Application.Repositories;

public interface ITeamRecordRepository
{
    Task<TeamRecord?> GetAsync(int id, CancellationToken cancellationToken = default);

    // List with optional text filter (matches Team or Mascot), pagination and ordering.
    // sortBy: allowed fields: Id, Team, Mascot, Rank, Wins, WinningPercentage, CreatedAt
    // sortDir: "asc" or "desc"
    Task<List<TeamRecord>> ListAsync(string? q = null, int page = 1, int pageSize = 20,
        string? sortBy = null, string sortDir = "asc", IEnumerable<string>? fields = null,
        CancellationToken cancellationToken = default);

    // Returns total count for an optional filter (used for pagination metadata)
    Task<int> CountAsync(string? q = null, IEnumerable<string>? fields = null,
        CancellationToken cancellationToken = default);

    Task<bool> RestoreAsync(int id, string? performedBy = null, CancellationToken cancellationToken = default);
    Task<bool> PurgeAsync(int id, CancellationToken cancellationToken = default);
    Task<int> PurgeAllSoftDeletedAsync(CancellationToken cancellationToken = default);
}