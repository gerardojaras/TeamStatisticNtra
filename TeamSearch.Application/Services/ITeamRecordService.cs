using TeamSearch.Shared.Dtos;

namespace TeamSearch.Application.Services;

public interface ITeamRecordService
{
    Task<TeamRecordDto?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<List<TeamRecordDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(int id, string performedBy, CancellationToken cancellationToken = default);
    Task<bool> PurgeAsync(int id, CancellationToken cancellationToken = default);
}

