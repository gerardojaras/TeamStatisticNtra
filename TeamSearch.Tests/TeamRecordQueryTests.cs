using TeamSearch.Shared;
using Xunit;

namespace TeamSearch.Tests;

public class TeamRecordQueryTests
{
    [Fact]
    public void DefaultValues_AreExpected()
    {
        var q = new TeamRecordQuery();
        Assert.Equal(1, q.Page);
        Assert.Equal(20, q.PageSize);
        Assert.Equal("asc", q.SortDir);
        Assert.Null(q.Search);
        Assert.Null(q.SortBy);
    }

    [Fact]
    public void CanSetProperties()
    {
        var q = new TeamRecordQuery
        {
            Search = "State University",
            Page = 2,
            PageSize = 50,
            SortBy = "Name",
            SortDir = "desc",
            Fields = "Name,Wins"
        };

        Assert.Equal("State University", q.Search);
        Assert.Equal(2, q.Page);
        Assert.Equal(50, q.PageSize);
        Assert.Equal("Name", q.SortBy);
        Assert.Equal("desc", q.SortDir);
        Assert.Equal("Name,Wins", q.Fields);
    }
}

