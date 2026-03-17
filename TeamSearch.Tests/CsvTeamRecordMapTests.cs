using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using FluentAssertions;
using TeamSearch.Seeder;
using TeamSearch.Shared.Dtos;
using Xunit;

namespace TeamSearch.Tests;

public class CsvTeamRecordMapTests
{
    [Fact]
    public void Parse_WellFormedRow_ParsesAllFields()
    {
        var csv = "1,Alpha,A,1/2/20,0.5,10,5,0,15\n";
        using var reader = new StringReader(csv);
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };
        using var csvr = new CsvReader(reader, cfg);
        csvr.Context.RegisterClassMap<CsvTeamRecordMap>();

        var records = csvr.GetRecords<CsvTeamRecord>().ToList();
        records.Should().HaveCount(1);
        var r = records[0];
        r.Rank.Should().Be(1);
        r.Team.Should().Be("Alpha");
        r.Mascot.Should().Be("A");
        r.DateOfLastWin.Should().NotBeNull();
        r.WinningPercentage.Should().Be(0.5m);
        r.Wins.Should().Be(10);
        r.Losses.Should().Be(5);
        r.Ties.Should().Be(0);
        r.Games.Should().Be(15);
    }

    [Fact]
    public void Parse_MissingOptionalFields_ParsesAndLeavesNulls()
    {
        var csv = ",State University,, , , , , , \n";
        using var reader = new StringReader(csv);
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };
        using var csvr = new CsvReader(reader, cfg);
        csvr.Context.RegisterClassMap<CsvTeamRecordMap>();

        var records = csvr.GetRecords<CsvTeamRecord>().ToList();
        records.Should().HaveCount(1);
        var r = records[0];
        r.Rank.Should().BeNull();
        r.Team.Should().Be("State University");
        r.Mascot.Should().BeNullOrWhiteSpace();
        r.DateOfLastWin.Should().BeNull();
        r.WinningPercentage.Should().BeNull();
        r.Wins.Should().BeNull();
    }

    [Fact]
    public void Parse_BadNumbers_DoesNotThrowAndLeavesNulls()
    {
        var csv = "x,Some Team,x,notadate,notdecimal,notint,notint,notint,notint\n";
        using var reader = new StringReader(csv);
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };
        using var csvr = new CsvReader(reader, cfg);
        csvr.Context.RegisterClassMap<CsvTeamRecordMap>();

        var records = csvr.GetRecords<CsvTeamRecord>().ToList();
        records.Should().HaveCount(1);
        var r = records[0];
        r.Rank.Should().BeNull();
        r.DateOfLastWin.Should().BeNull();
        r.WinningPercentage.Should().BeNull();
        r.Wins.Should().BeNull();
    }
}