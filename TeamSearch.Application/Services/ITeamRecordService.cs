using TeamSearch.Shared.Dtos;

namespace TeamSearch.Application.Services;

public interface ITeamRecordService
{
    Task<TeamRecordDto?> GetAsync(int id, CancellationToken cancellationToken = default);

    // List with optional text filter and pagination
    Task<List<TeamRecordDto>> ListAsync(string? q = null, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default);

    // Total count for optional filter
    Task<int> CountAsync(string? q = null, CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(int id, string? performedBy, CancellationToken cancellationToken = default);
    Task<bool> PurgeAsync(int id, CancellationToken cancellationToken = default);
}