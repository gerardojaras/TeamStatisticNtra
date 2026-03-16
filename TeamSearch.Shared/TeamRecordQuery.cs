namespace TeamSearch.Shared;

public sealed class TeamRecordQuery
{
    public string? Search { get; set; }

    // NOTE: Removed obsolete alias `Q` — callers should use `Search` instead.
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public string SortDir { get; set; } = "asc";
    public string? Fields { get; set; }
}