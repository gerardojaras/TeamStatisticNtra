namespace TeamSearch.Shared.Dtos;

public sealed class TeamRecordDto
{
    public int Id { get; set; }
    public int? Rank { get; set; }
    public string Team { get; set; } = string.Empty;
    public string? Mascot { get; set; }
    public DateTime? DateOfLastWin { get; set; }
    public decimal? WinningPercentage { get; set; }
    public int? Wins { get; set; }
    public int? Losses { get; set; }
    public int? Ties { get; set; }
    public int? Games { get; set; }
}

