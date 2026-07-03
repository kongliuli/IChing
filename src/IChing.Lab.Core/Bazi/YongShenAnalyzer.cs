namespace IChing.Lab.Core.Bazi;

/// <summary>ponytail: 月令得令 + 格局细分用神，Lab 级启发式。</summary>
public static class YongShenAnalyzer
{
    private static readonly Dictionary<string, string> GanWuXing = new()
    {
        ["甲"] = "木", ["乙"] = "木", ["丙"] = "火", ["丁"] = "火",
        ["戊"] = "土", ["己"] = "土", ["庚"] = "金", ["辛"] = "金",
        ["壬"] = "水", ["癸"] = "水"
    };

    private static readonly Dictionary<string, string> ZhiWuXing = new()
    {
        ["子"] = "水", ["丑"] = "土", ["寅"] = "木", ["卯"] = "木",
        ["辰"] = "土", ["巳"] = "火", ["午"] = "火", ["未"] = "土",
        ["申"] = "金", ["酉"] = "金", ["戌"] = "土", ["亥"] = "水"
    };

    private static readonly Dictionary<string, string> Generates = new()
    {
        ["木"] = "火", ["火"] = "土", ["土"] = "金", ["金"] = "水", ["水"] = "木"
    };

    private static readonly Dictionary<string, string> Overcomes = new()
    {
        ["木"] = "土", ["土"] = "水", ["水"] = "火", ["火"] = "金", ["金"] = "木"
    };

    private static readonly HashSet<string> BiJie = ["比肩", "劫财"];
    private static readonly HashSet<string> Yin = ["正印", "偏印"];
    private static readonly HashSet<string> ShiShang = ["食神", "伤官"];
    private static readonly HashSet<string> Cai = ["正财", "偏财"];
    private static readonly HashSet<string> GuanSha = ["正官", "七杀"];

    public static YongShenProfile Analyze(BaziChart chart)
    {
        var dmWx = GanWuXing[chart.DayMaster];
        var strength = JudgeStrength(dmWx, ZhiWuXing[chart.MonthPillar.Zhi]);
        var geJu = GeJuAnalyzer.Analyze(chart, strength);
        var geJuBreak = GeJuPoGeDetector.Detect(chart, geJu, strength);
        var shiShenCounts = CountShiShenCategories(chart);

        return new YongShenProfile(
            strength,
            geJu with { Break = geJuBreak },
            geJu.PrimaryYongShen,
            geJu.SecondaryYongShen,
            geJu.FavoredCategories,
            geJu.FavoredElements,
            shiShenCounts,
            $"{geJu.Summary}；{geJuBreak.Summary}；主用神{geJu.PrimaryYongShen}" +
            (geJu.SecondaryYongShen is null ? "" : $"，辅用神{geJu.SecondaryYongShen}"));
    }

    private static string JudgeStrength(string dmWx, string monthWx)
    {
        if (dmWx == monthWx)
        {
            return "身强（得令）";
        }

        if (Generates.TryGetValue(monthWx, out var gen) && gen == dmWx)
        {
            return "身强（月令生扶）";
        }

        if (Overcomes.TryGetValue(monthWx, out var ov) && ov == dmWx)
        {
            return "身弱（月令克身）";
        }

        if (Generates.TryGetValue(dmWx, out var leak) && leak == monthWx)
        {
            return "身弱（泄气于月令）";
        }

        return "身中和";
    }

    private static IReadOnlyDictionary<string, int> CountShiShenCategories(BaziChart chart)
    {
        var counts = new Dictionary<string, int>
        {
            ["比劫"] = 0, ["印星"] = 0, ["食伤"] = 0, ["财星"] = 0, ["官杀"] = 0
        };

        foreach (var pillar in new[] { chart.YearPillar, chart.MonthPillar, chart.DayPillar, chart.HourPillar })
        {
            Bump(counts, pillar.ShiShenGan);
            foreach (var z in pillar.ShiShenZhi)
            {
                Bump(counts, z);
            }
        }

        return counts;
    }

    private static void Bump(Dictionary<string, int> counts, string shiShen)
    {
        if (BiJie.Contains(shiShen))
        {
            counts["比劫"]++;
        }
        else if (Yin.Contains(shiShen))
        {
            counts["印星"]++;
        }
        else if (ShiShang.Contains(shiShen))
        {
            counts["食伤"]++;
        }
        else if (Cai.Contains(shiShen))
        {
            counts["财星"]++;
        }
        else if (GuanSha.Contains(shiShen))
        {
            counts["官杀"]++;
        }
    }
}

public record YongShenProfile(
    string Strength,
    GeJuInfo GeJu,
    string PrimaryYongShen,
    string? SecondaryYongShen,
    IReadOnlyList<string> FavoredCategories,
    IReadOnlyList<string> FavoredElements,
    IReadOnlyDictionary<string, int> ShiShenCounts,
    string Summary);
