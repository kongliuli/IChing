namespace IChing.Lab.Core.Bazi;

public static class HePanService
{
    private static readonly Dictionary<string, string> GanWuXing = new()
    {
        ["甲"] = "木", ["乙"] = "木",
        ["丙"] = "火", ["丁"] = "火",
        ["戊"] = "土", ["己"] = "土",
        ["庚"] = "金", ["辛"] = "金",
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

    public static HePanResult Compare(BaziChart a, BaziChart b)
    {
        var dayRelation = DescribeRelation(a.DayMaster, b.DayMaster);
        var elementOverlap = CountElementOverlap(a.WuXingSummary.Counts, b.WuXingSummary.Counts);
        var complement = DescribeComplement(a.WuXingSummary, b.WuXingSummary);
        var zhiPairs = CheckZhiPairs(a, b);
        var score = Score(dayRelation, elementOverlap, complement, zhiPairs);

        return new HePanResult(
            PersonA: Summarize(a),
            PersonB: Summarize(b),
            DayMasterRelation: dayRelation,
            ElementOverlap: elementOverlap,
            ElementComplement: complement,
            ZhiPairs: zhiPairs,
            Score: score,
            Summary: BuildSummary(a.DayMaster, b.DayMaster, dayRelation, score, zhiPairs));
    }

    private static PersonSummary Summarize(BaziChart c) =>
        new(c.DayMaster, c.DayPillar.GanZhi, c.WuXingSummary.Dominant);

    private static string DescribeRelation(string ganA, string ganB)
    {
        if (!GanWuXing.TryGetValue(ganA, out var wxA) || !GanWuXing.TryGetValue(ganB, out var wxB))
        {
            return "未知";
        }

        if (wxA == wxB)
        {
            return "同五行（比肩类）";
        }

        if (Generates.TryGetValue(wxA, out var gen) && gen == wxB)
        {
            return $"相生（{wxA}生{wxB}）";
        }

        if (Generates.TryGetValue(wxB, out var genB) && genB == wxA)
        {
            return $"相生（{wxB}生{wxA}）";
        }

        if (Overcomes.TryGetValue(wxA, out var ov) && ov == wxB)
        {
            return $"相克（{wxA}克{wxB}）";
        }

        if (Overcomes.TryGetValue(wxB, out var ovB) && ovB == wxA)
        {
            return $"相克（{wxB}克{wxA}）";
        }

        return "中性";
    }

    private static int CountElementOverlap(
        IReadOnlyDictionary<string, int> a,
        IReadOnlyDictionary<string, int> b)
    {
        var overlap = 0;
        foreach (var (element, countA) in a)
        {
            if (b.TryGetValue(element, out var countB))
            {
                overlap += Math.Min(countA, countB);
            }
        }

        return overlap;
    }

    private static string DescribeComplement(WuXingSummary a, WuXingSummary b)
    {
        var weakA = FindWeakElements(a.Counts);
        var weakB = FindWeakElements(b.Counts);
        var fills = weakA.Count(e => b.Counts.GetValueOrDefault(e) >= 2)
                    + weakB.Count(e => a.Counts.GetValueOrDefault(e) >= 2);
        return fills switch
        {
            >= 2 => "五行互补明显",
            1 => "部分五行互补",
            _ => "五行互补有限"
        };
    }

    private static IReadOnlyList<string> FindWeakElements(IReadOnlyDictionary<string, int> counts) =>
        counts.Where(kv => kv.Value <= 1).Select(kv => kv.Key).ToList();

    private static IReadOnlyList<string> CheckZhiPairs(BaziChart a, BaziChart b)
    {
        var zhiA = new[] { a.YearPillar.Zhi, a.MonthPillar.Zhi, a.DayPillar.Zhi, a.HourPillar.Zhi };
        var zhiB = new[] { b.YearPillar.Zhi, b.MonthPillar.Zhi, b.DayPillar.Zhi, b.HourPillar.Zhi };
        var tags = new List<string>();

        foreach (var za in zhiA)
        {
            foreach (var zb in zhiB)
            {
                if (IsLiuHe(za, zb))
                {
                    tags.Add($"{za}{zb}六合");
                }

                if (IsChong(za, zb))
                {
                    tags.Add($"{za}{zb}相冲");
                }
            }
        }

        return tags.Distinct().ToList();
    }

    private static bool IsLiuHe(string a, string b) => (a, b) switch
    {
        ("子", "丑") or ("丑", "子") => true,
        ("寅", "亥") or ("亥", "寅") => true,
        ("卯", "戌") or ("戌", "卯") => true,
        ("辰", "酉") or ("酉", "辰") => true,
        ("巳", "申") or ("申", "巳") => true,
        ("午", "未") or ("未", "午") => true,
        _ => false
    };

    private static bool IsChong(string a, string b) => (a, b) switch
    {
        ("子", "午") or ("午", "子") => true,
        ("丑", "未") or ("未", "丑") => true,
        ("寅", "申") or ("申", "寅") => true,
        ("卯", "酉") or ("酉", "卯") => true,
        ("辰", "戌") or ("戌", "辰") => true,
        ("巳", "亥") or ("亥", "巳") => true,
        _ => false
    };

    private static int Score(string dayRelation, int overlap, string complement, IReadOnlyList<string> zhiPairs)
    {
        var score = 50;
        if (dayRelation.Contains("相生"))
        {
            score += 15;
        }
        else if (dayRelation.Contains("同五行"))
        {
            score += 8;
        }
        else if (dayRelation.Contains("相克"))
        {
            score -= 10;
        }

        score += Math.Min(overlap * 3, 12);
        if (complement.Contains("明显"))
        {
            score += 10;
        }
        else if (complement.Contains("部分"))
        {
            score += 5;
        }

        score += zhiPairs.Count(t => t.Contains("六合")) * 5;
        score -= zhiPairs.Count(t => t.Contains("相冲")) * 4;
        return Math.Clamp(score, 0, 100);
    }

    private static string BuildSummary(
        string dmA, string dmB, string relation, int score, IReadOnlyList<string> zhiPairs)
    {
        var zhiNote = zhiPairs.Count > 0 ? $"地支见{string.Join("、", zhiPairs)}。" : "";
        return $"日主{dmA}与{dmB}，{relation}。合盘指数 {score}/100。{zhiNote}";
    }
}

public record PersonSummary(string DayMaster, string DayPillar, string DominantElement);

public record HePanResult(
    PersonSummary PersonA,
    PersonSummary PersonB,
    string DayMasterRelation,
    int ElementOverlap,
    string ElementComplement,
    IReadOnlyList<string> ZhiPairs,
    int Score,
    string Summary);
