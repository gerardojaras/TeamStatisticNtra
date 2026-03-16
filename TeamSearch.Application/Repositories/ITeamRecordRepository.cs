using TeamSearch.Domain;

namespace TeamSearch.Application.Repositories;

public interface ITeamRecordRepository
{
    Task<TeamRecord?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<List<TeamRecord>> ListAsync(CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(int id, string? performedBy = null, CancellationToken cancellationToken = default);
    Task<bool> PurgeAsync(int id, CancellationToken cancellationToken = default);
    Task<int> PurgeAllSoftDeletedAsync(CancellationToken cancellationToken = default);
}