using Microsoft.FluentUI.AspNetCore.Components;
using TeamSearch.Shared.Dtos;

namespace TeamSearch.Client.Services;

public sealed class TeamRecordPresentationService : ITeamRecordPresentationService
{
    private static readonly IReadOnlyList<string> _defaultSelectedColumns =
        ["Id", "Team", "Mascot", "Wins", "WinningPercentage", "CreatedAt"];

    private static readonly IReadOnlyDictionary<string, string> _allColumns =
        new Dictionary<string, string>
        {
            ["Id"] = "ID",
            ["Rank"] = "Rank",
            ["Team"] = "Team",
            ["Mascot"] = "Mascot",
            ["DateOfLastWin"] = "Date of Last Win",
            ["WinningPercentage"] = "Winning %",
            ["Wins"] = "Wins",
            ["Losses"] = "Losses",
            ["Ties"] = "Ties",
            ["Games"] = "Games",
            ["CreatedAt"] = "Created At"
        };

    public IReadOnlyDictionary<string, string> AllColumns => _allColumns;
    public IReadOnlyList<string> DefaultSelectedColumns => _defaultSelectedColumns;

    public string GetColumnString(TeamRecordDto record, string columnKey)
    {
        return columnKey switch
        {
            "Id" => record.Id.ToString(),
            "Rank" => record.Rank?.ToString() ?? string.Empty,
            "Team" => record.Team,
            "Mascot" => record.Mascot ?? string.Empty,
            "DateOfLastWin" => record.DateOfLastWin?.ToString("yyyy-MM-dd") ?? string.Empty,
            "WinningPercentage" => record.WinningPercentage?.ToString() ?? string.Empty,
            "Wins" => record.Wins?.ToString() ?? string.Empty,
            "Losses" => record.Losses?.ToString() ?? string.Empty,
            "Ties" => record.Ties?.ToString() ?? string.Empty,
            "Games" => record.Games?.ToString() ?? string.Empty,
            "CreatedAt" => record.CreatedAt.ToString("yyyy-MM-dd"),
            _ => string.Empty
        };
    }

    public GridSort<TeamRecordDto>? GetSortExpression(string columnKey)
    {
        return columnKey switch
        {
            "Id" => GridSort<TeamRecordDto>.ByAscending(x => x.Id),
            "Rank" => GridSort<TeamRecordDto>.ByAscending(x => x.Rank),
            "Team" => GridSort<TeamRecordDto>.ByAscending(x => x.Team),
            "Mascot" => GridSort<TeamRecordDto>.ByAscending(x => x.Mascot),
            "DateOfLastWin" => GridSort<TeamRecordDto>.ByAscending(x => x.DateOfLastWin),
            "WinningPercentage" => GridSort<TeamRecordDto>.ByAscending(x => x.WinningPercentage),
            "Wins" => GridSort<TeamRecordDto>.ByAscending(x => x.Wins),
            "Losses" => GridSort<TeamRecordDto>.ByAscending(x => x.Losses),
            "Ties" => GridSort<TeamRecordDto>.ByAscending(x => x.Ties),
            "Games" => GridSort<TeamRecordDto>.ByAscending(x => x.Games),
            "CreatedAt" => GridSort<TeamRecordDto>.ByAscending(x => x.CreatedAt),
            _ => null
        };
    }
}

