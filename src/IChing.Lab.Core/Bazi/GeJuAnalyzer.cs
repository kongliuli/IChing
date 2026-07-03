namespace IChing.Lab.Core.Bazi;

/// <summary>ponytail: 月令十神定正格 + 五行占比判从/专旺，Lab 级格局细分。</summary>
public static class GeJuAnalyzer
{
    private static readonly Dictionary<string, string> GanWuXing = new()
    {
        ["甲"] = "木", ["乙"] = "木", ["丙"] = "火", ["丁"] = "火",
        ["戊"] = "土", ["己"] = "土", ["庚"] = "金", ["辛"] = "金",
        ["壬"] = "水", ["癸"] = "水"
    };

    private static readonly Dictionary<string, string> Generates = new()
    {
        ["木"] = "火", ["火"] = "土", ["土"] = "金", ["金"] = "水", ["水"] = "木"
    };

    public static GeJuInfo Analyze(BaziChart chart, string strength)
    {
        var dmWx = GanWuXing[chart.DayMaster];
        var counts = CountElements(chart);
        var dmCount = counts.GetValueOrDefault(dmWx);
        var dominant = counts.OrderByDescending(kv => kv.Value).First();

        if (dominant.Key != dmWx && dominant.Value >= 3 && dmCount <= 1)
        {
            return new GeJuInfo(
                $"从{dominant.Key}格",
                "从格",
                dominant.Key,
                null,
                ["食伤", "财星", "官杀"],
                [dominant.Key],
                $"日主极弱，{dominant.Key}势专旺，顺势而从");
        }

        if (dmCount >= 3)
        {
            return new GeJuInfo(
                $"专旺{dmWx}格",
                "专旺",
                dmWx,
                "印星",
                ["印星", "比劫", "食伤"],
                [dmWx, Generates.GetValueOrDefault(dmWx, dmWx)],
                $"日主{dmWx}气专旺（{strength}），忌官杀");
        }

        return BuildRegular(chart.MonthPillar.ShiShenGan, strength, chart.DayMaster);
    }

    private static Dictionary<string, int> CountElements(BaziChart chart)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var wx in new[]
                 {
                     chart.YearPillar.WuXing, chart.MonthPillar.WuXing,
                     chart.DayPillar.WuXing, chart.HourPillar.WuXing
                 })
        {
            counts.TryGetValue(wx, out var n);
            counts[wx] = n + 1;
        }

        return counts;
    }

    private static GeJuInfo BuildRegular(string monthShiShen, string strength, string dayMaster)
    {
        var pattern = monthShiShen switch
        {
            "正官" => "正官格",
            "七杀" => "七杀格",
            "正财" or "偏财" => "财格",
            "正印" or "偏印" => "印格",
            "食神" => "食神格",
            "伤官" => "伤官格",
            "比肩" or "劫财" => "比劫格",
            _ => "普通格局"
        };

        var isStrong = strength.Contains("强");
        var (primary, secondary, categories) = pattern switch
        {
            "正官格" => isStrong
                ? ("财星", "食伤", (IReadOnlyList<string>)["财星", "食伤"])
                : ("印星", "比劫", ["印星", "比劫"]),
            "七杀格" => ("印星", "食神", ["印星", "食神"]),
            "财格" => isStrong
                ? ("官杀", "食伤", ["官杀", "食伤"])
                : ("比劫", "印星", ["比劫", "印星"]),
            "印格" => isStrong
                ? ("财星", "官杀", ["财星", "官杀"])
                : ("官杀", "比劫", ["官杀", "比劫"]),
            "食神格" => ("财星", "比劫", ["财星", "比劫"]),
            "伤官格" => ("财星", "印星", ["财星", "印星"]),
            "比劫格" => isStrong
                ? ("官杀", "食伤", ["官杀", "食伤"])
                : ("印星", "比劫", ["印星", "比劫"]),
            _ => isStrong
                ? ("官杀", "食伤", ["官杀", "食伤"])
                : ("印星", "比劫", ["印星", "比劫"])
        };

        var elements = ShiShenElementMapper.MapCategories(dayMaster, categories);
        return new GeJuInfo(
            pattern,
            "正格",
            primary,
            secondary,
            categories,
            elements,
            $"月令取{monthShiShen}，成{pattern}，{strength}");
    }
}

public record GeJuInfo(
    string Pattern,
    string Category,
    string PrimaryYongShen,
    string? SecondaryYongShen,
    IReadOnlyList<string> FavoredCategories,
    IReadOnlyList<string> FavoredElements,
    string Summary);
