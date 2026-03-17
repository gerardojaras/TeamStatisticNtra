using Microsoft.FluentUI.AspNetCore.Components;
using TeamSearch.Shared.Dtos;

namespace TeamSearch.Client.Services;

public interface ITeamRecordPresentationService
{
    IReadOnlyDictionary<string, string> AllColumns { get; }
    IReadOnlyList<string> DefaultSelectedColumns { get; }

    string GetColumnString(TeamRecordDto record, string columnKey);
    GridSort<TeamRecordDto>? GetSortExpression(string columnKey);
}

