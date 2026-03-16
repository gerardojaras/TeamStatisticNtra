using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using TeamSearch.Shared.Dtos;

namespace TeamSearch.Seeder;

public sealed class CsvTeamRecordMap : ClassMap<CsvTeamRecord>
{
    public CsvTeamRecordMap()
    {
        // Use index mapping because the CSV headers/rows are inconsistent in this file
        Map(m => m.Rank).Index(0).TypeConverter<NullableIntConverter>();
        Map(m => m.Team).Index(1);
        Map(m => m.Mascot).Index(2);
        Map(m => m.DateOfLastWin).Index(3).TypeConverter<LenientNullableDateConverter>();
        Map(m => m.WinningPercentage).Index(4).TypeConverter<LenientNullableDecimalConverter>();
        Map(m => m.Wins).Index(5).TypeConverter<NullableIntConverter>();
        Map(m => m.Losses).Index(6).TypeConverter<NullableIntConverter>();
        Map(m => m.Ties).Index(7).TypeConverter<NullableIntConverter>();
        Map(m => m.Games).Index(8).TypeConverter<NullableIntConverter>();
    }
}

internal sealed class LenientNullableDateConverter : DefaultTypeConverter
{
    private static readonly string[] Formats = new[] { "M/d/yy", "M/d/yyyy", "MM/dd/yy", "MM/dd/yyyy", "yyyy-MM-dd" };

    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (DateTime.TryParseExact(text.Trim(), Formats, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var dt)) return dt;
        if (DateTime.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt)) return dt;
        return null;
    }
}

internal sealed class LenientNullableDecimalConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (decimal.TryParse(text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
        return null;
    }
}

internal sealed class NullableIntConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) return v;
        return null;
    }
}