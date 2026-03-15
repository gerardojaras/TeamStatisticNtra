namespace TeamSearch.Domain;

public sealed class TeamRecord
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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
