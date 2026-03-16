using System.Net;
using TeamSearch.Application.Mappings;
using TeamSearch.Application.Repositories;
using TeamSearch.Shared;
using TeamSearch.Shared.Dtos;

namespace TeamSearch.Application.Services;

public class TeamRecordService(ITeamRecordRepository repo) : ITeamRecordService
{
    private readonly ITeamRecordRepository _repository = repo ?? throw new ArgumentNullException(nameof(repo));

    public async Task<TeamRecordDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var teamRecordEntity = await _repository.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return teamRecordEntity == null ? null : teamRecordEntity.ToDto();
    }

    public async Task<List<TeamRecordDto>> ListAsync(string? q = null, int page = 1, int pageSize = 20,
        string? sortBy = null, string sortDir = "asc", CancellationToken cancellationToken = default)
    {
        var records = await _repository.ListAsync(q, page, pageSize, sortBy, sortDir, null, cancellationToken)
            .ConfigureAwait(false);
        var result = records.Select(r => r.ToDto()).ToList();
        return result;
    }

    public async Task<List<TeamRecordDto>> ListAsync(TeamRecordQuery requestQuery,
        CancellationToken cancellationToken = default)
    {
        if (requestQuery == null) throw new ArgumentNullException(nameof(requestQuery));

        var decodedQuery = string.IsNullOrWhiteSpace(requestQuery.Search)
            ? null
            : WebUtility.UrlDecode(requestQuery.Search).Trim();
        var decodedSortByField = string.IsNullOrWhiteSpace(requestQuery.SortBy)
            ? null
            : WebUtility.UrlDecode(requestQuery.SortBy).Trim();
        var sortDirection = requestQuery.SortDir.ToLowerInvariant() == "desc" ? "desc" : "asc";

        var requestedFields = string.IsNullOrWhiteSpace(requestQuery.Fields)
            ? null
            : requestQuery.Fields.Split(',').Select(f => f.Trim()).Where(f => !string.IsNullOrEmpty(f));

        var recordsFromRepo = await _repository.ListAsync(decodedQuery, requestQuery.Page, requestQuery.PageSize,
            decodedSortByField, sortDirection, requestedFields, cancellationToken).ConfigureAwait(false);
        var mapped = recordsFromRepo.Select(r => r.ToDto()).ToList();
        return mapped;
    }

    public Task<int> CountAsync(string? q = null, CancellationToken cancellationToken = default)
    {
        return _repository.CountAsync(q, null, cancellationToken);
    }

    public Task<int> CountAsync(TeamRecordQuery requestQuery, CancellationToken cancellationToken = default)
    {
        if (requestQuery == null) throw new ArgumentNullException(nameof(requestQuery));
        var decodedQuery = string.IsNullOrWhiteSpace(requestQuery.Search)
            ? null
            : WebUtility.UrlDecode(requestQuery.Search).Trim();
        var requestedFields = string.IsNullOrWhiteSpace(requestQuery.Fields)
            ? null
            : requestQuery.Fields.Split(',').Select(f => f.Trim()).Where(f => !string.IsNullOrEmpty(f));

        return _repository.CountAsync(decodedQuery, requestedFields, cancellationToken);
    }

    public Task<bool> RestoreAsync(int id, string? performedBy, CancellationToken cancellationToken = default)
    {
        return _repository.RestoreAsync(id, performedBy, cancellationToken);
    }

    public Task<bool> PurgeAsync(int id, CancellationToken cancellationToken = default)
    {
        return _repository.PurgeAsync(id, cancellationToken);
    }

    // Mapping is provided by TeamRecordMappings.ToDto extension method in Application.Mappings
}