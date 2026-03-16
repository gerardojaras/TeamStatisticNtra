using TeamSearch.Domain;
using TeamSearch.Shared.Dtos;

namespace TeamSearch.Application.Mappings;

public static class TeamRecordMappings
{
    public static TeamRecordDto ToDto(this TeamRecord src)
    {
        return new TeamRecordDto
        {
            Id = src.Id,
            Rank = src.Rank,
            Team = src.Team?.Trim() ?? string.Empty,
            Mascot = src.Mascot?.Trim(),
            DateOfLastWin = src.DateOfLastWin,
            WinningPercentage = src.WinningPercentage,
            Wins = src.Wins,
            Losses = src.Losses,
            Ties = src.Ties,
            Games = src.Games,
        };
    }
}

