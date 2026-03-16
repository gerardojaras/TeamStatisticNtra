using TeamSearch.Shared;
using TeamSearch.Shared.Dtos;

namespace TeamSearch.Client.Services;

public interface ITeamRecordClient
{
    Task<ApiResponse<List<TeamRecordDto>>> ListAsync(string? search = null, string? fields = null, int page = 1,
        int pageSize = 20, string? sortBy = null, string? sortDir = null,
        CancellationToken cancellationToken = default);
}