using TeamSearch.Shared;
using TeamSearch.Shared.Dtos;

namespace TeamSearch.Application.Services;

public interface ITeamRecordService
{
    Task<TeamRecordDto?> GetAsync(int id, CancellationToken cancellationToken = default);

    // List with optional text filter, pagination and ordering
    Task<List<TeamRecordDto>> ListAsync(string? q = null, int page = 1, int pageSize = 20,
        string? sortBy = null, string sortDir = "asc", CancellationToken cancellationToken = default);

    // New: List using a query object (preferred for controller model binding)
    Task<List<TeamRecordDto>> ListAsync(TeamRecordQuery query, CancellationToken cancellationToken = default);

    // Total count for optional filter
    Task<int> CountAsync(string? q = null, CancellationToken cancellationToken = default);

    // New: Count using a query object
    Task<int> CountAsync(TeamRecordQuery query, CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(int id, string? performedBy, CancellationToken cancellationToken = default);
    Task<bool> PurgeAsync(int id, CancellationToken cancellationToken = default);
}