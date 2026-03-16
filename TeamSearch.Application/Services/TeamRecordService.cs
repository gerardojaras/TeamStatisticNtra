using TeamSearch.Application.Repositories;
using TeamSearch.Domain;
using TeamSearch.Shared.Dtos;

namespace TeamSearch.Application.Services;

public class TeamRecordService(ITeamRecordRepository repo) : ITeamRecordService
{
    private readonly ITeamRecordRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));

    public async Task<TeamRecordDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return entity == null ? null : Map(entity);
    }

    public async Task<List<TeamRecordDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repo.ListAsync(cancellationToken).ConfigureAwait(false);
        return list.Select(Map).ToList();
    }

    public Task<bool> RestoreAsync(int id, string performedBy, CancellationToken cancellationToken = default)
    {
        return _repo.RestoreAsync(id, performedBy, cancellationToken);
    }

    public Task<bool> PurgeAsync(int id, CancellationToken cancellationToken = default)
    {
        return _repo.PurgeAsync(id, cancellationToken);
    }

    private static TeamRecordDto Map(TeamRecord e)
    {
        return new TeamRecordDto
        {
            Id = e.Id,
            Rank = e.Rank,
            Team = e.Team,
            Mascot = e.Mascot,
            DateOfLastWin = e.DateOfLastWin,
            WinningPercentage = e.WinningPercentage,
            Wins = e.Wins,
            Losses = e.Losses,
            Ties = e.Ties,
            Games = e.Games,
            CreatedAt = e.CreatedAt,
            CreatedBy = e.CreatedBy,
            LastModifiedAt = e.LastModifiedAt,
            LastModifiedBy = e.LastModifiedBy,
            IsDeleted = e.IsDeleted,
            DeletedAt = e.DeletedAt,
            DeletedBy = e.DeletedBy
        };
    }
}