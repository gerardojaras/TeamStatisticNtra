using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TeamSearch.Application.Repositories;
using TeamSearch.Application.Services;
using TeamSearch.Domain;
using TeamSearch.Shared;
using Xunit;

namespace TeamSearch.Tests;

public class TeamRecordServiceTests
{
    [Fact]
    public async Task ListAsync_WhenRecordsExist_ReturnsMappedDtos()
    {
        // Arrange
        var domainRecords = new[]
        {
            new TeamRecord { Id = 1, Team = "Alpha", Mascot = "A", Wins = 10, WinningPercentage = 0.5m },
            new TeamRecord { Id = 2, Team = "Beta", Mascot = "B", Wins = 8, WinningPercentage = 0.44m }
        };

        var repo = new Mock<ITeamRecordRepository>();
        repo.Setup(r => r.ListAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(domainRecords.ToList());

        var service = new TeamRecordService(repo.Object);

        // Act
        var result = await service.ListAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Team.Should().Be("Alpha");
        result[1].Wins.Should().Be(8);
        repo.Verify(
            r => r.ListAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var repo = new Mock<ITeamRecordRepository>();
        repo.Setup(r => r.GetAsync(123, It.IsAny<CancellationToken>())).ReturnsAsync((TeamRecord?)null);

        var service = new TeamRecordService(repo.Object);

        // Act
        var result = await service.GetAsync(123);

        // Assert
        result.Should().BeNull();
        repo.Verify(r => r.GetAsync(123, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_WithQuery_DecodesAndPassesValuesToRepository()
    {
        // Arrange
        var repo = new Mock<ITeamRecordRepository>();
        repo.Setup(r => r.ListAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamRecord>());

        var service = new TeamRecordService(repo.Object);

        var query = new TeamRecordQuery
        {
            Search = WebUtility.UrlEncode("State University"),
            SortBy = WebUtility.UrlEncode(" Team "),
            SortDir = "DESC",
            Page = 2,
            PageSize = 50,
            Fields = "Team, Mascot ,Wins"
        };

        var expectedSearch = "State University";
        var expectedSortBy = "Team";
        var expectedFields = new[] { "Team", "Mascot", "Wins" };

        // Act
        var result = await service.ListAsync(query);

        // Assert
        repo.Verify(r => r.ListAsync(
            It.Is<string?>(s => s == expectedSearch),
            It.Is<int>(p => p == 2),
            It.Is<int>(ps => ps == 50),
            It.Is<string?>(sb => sb == expectedSortBy),
            It.Is<string>(dir => dir == "desc"),
            It.Is<IEnumerable<string>?>(fields => fields != null && fields.SequenceEqual(expectedFields)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CountAsync_WithQuery_DecodesAndPassesValuesToRepository()
    {
        // Arrange
        var repo = new Mock<ITeamRecordRepository>();
        repo.Setup(r =>
                r.CountAsync(It.IsAny<string?>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var service = new TeamRecordService(repo.Object);

        var query = new TeamRecordQuery
        {
            Search = WebUtility.UrlEncode("  State%20X  "),
            Fields = " Team , Wins "
        };

        var expectedSearch = WebUtility.UrlDecode(query.Search).Trim();
        var expectedFields = new[] { "Team", "Wins" };

        // Act
        var count = await service.CountAsync(query);

        // Assert
        count.Should().Be(42);
        repo.Verify(r => r.CountAsync(
            It.Is<string?>(s => s == expectedSearch),
            It.Is<IEnumerable<string>?>(fields => fields != null && fields.SequenceEqual(expectedFields)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_NullQuery_ThrowsArgumentNullException()
    {
        var repo = new Mock<ITeamRecordRepository>();
        var service = new TeamRecordService(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await service.ListAsync((TeamRecordQuery?)null!));
    }

    [Fact]
    public async Task ListAsync_WhitespaceSearch_IsTreatedAsNull()
    {
        var repo = new Mock<ITeamRecordRepository>();
        repo.Setup(r => r.ListAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamRecord>());

        var service = new TeamRecordService(repo.Object);

        var query = new TeamRecordQuery { Search = "   ", Page = 1, PageSize = 10 };

        await service.ListAsync(query);

        repo.Verify(
            r => r.ListAsync(It.Is<string?>(s => s == null), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_EmptyFields_StringIsNull_TreatedAsNull()
    {
        var repo = new Mock<ITeamRecordRepository>();
        repo.Setup(r => r.ListAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamRecord>());

        var service = new TeamRecordService(repo.Object);

        var query = new TeamRecordQuery { Fields = "   ", Page = 1, PageSize = 10 };

        await service.ListAsync(query);

        // whitespace-only Fields should be treated as null
        repo.Verify(
            r => r.ListAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.Is<IEnumerable<string>?>(f => f == null), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ListAsync_FieldsWithEmptyItems_RemovesEmptyEntriesAndTrims()
    {
        var repo = new Mock<ITeamRecordRepository>();
        repo.Setup(r => r.ListAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamRecord>());

        var service = new TeamRecordService(repo.Object);

        var query = new TeamRecordQuery { Fields = " Team , , Mascot ,, Wins ", Page = 1, PageSize = 10 };
        var expected = new[] { "Team", "Mascot", "Wins" };

        await service.ListAsync(query);

        repo.Verify(
            r => r.ListAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.Is<IEnumerable<string>?>(f => f != null && f.SequenceEqual(expected)),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_SortDir_InvalidDefaultsToAsc_CaseInsensitiveDescAccepted()
    {
        var repo = new Mock<ITeamRecordRepository>();
        repo.Setup(r => r.ListAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TeamRecord>());

        var service = new TeamRecordService(repo.Object);

        var q1 = new TeamRecordQuery { SortDir = "invalid" };
        await service.ListAsync(q1);
        repo.Verify(
            r => r.ListAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.Is<string>(d => d == "asc"), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);

        var q2 = new TeamRecordQuery { SortDir = "DeSc" };
        await service.ListAsync(q2);
        repo.Verify(
            r => r.ListAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.Is<string>(d => d == "desc"), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }
}