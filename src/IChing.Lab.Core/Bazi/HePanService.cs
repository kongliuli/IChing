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
        var dayRelation = DescribeGanRelation(a.DayMaster, b.DayMaster);
        var elementOverlap = CountElementOverlap(a.WuXingSummary.Counts, b.WuXingSummary.Counts);
        var complement = DescribeComplement(a.WuXingSummary, b.WuXingSummary);
        var zhiPairs = CheckZhiPairs(a, b);
        var dayNaYin = NaYinHelper.DescribeRelation(a.DayPillar.NaYin, b.DayPillar.NaYin);
        var yearNaYin = NaYinHelper.DescribeRelation(a.YearPillar.NaYin, b.YearPillar.NaYin);
        var yongShenComplement = DescribeYongShenComplement(a, b);
        var score = Score(dayRelation, elementOverlap, complement, zhiPairs, dayNaYin, yongShenComplement);

        return new HePanResult(
            PersonA: Summarize(a),
            PersonB: Summarize(b),
            DayMasterRelation: dayRelation,
            DayNaYinRelation: dayNaYin,
            YearNaYinRelation: yearNaYin,
            ElementOverlap: elementOverlap,
            ElementComplement: complement,
            YongShenComplement: yongShenComplement,
            ZhiPairs: zhiPairs,
            Score: score,
            Summary: BuildSummary(a, b, dayRelation, dayNaYin, yongShenComplement, score, zhiPairs));
    }

    private static PersonSummary Summarize(BaziChart c) =>
        new(c.DayMaster, c.DayPillar.GanZhi, c.DayPillar.NaYin,
            c.WuXingSummary.Dominant, c.YongShen.Strength, c.YongShen.FavoredElements);

    private static string DescribeGanRelation(string ganA, string ganB)
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

    private static string DescribeYongShenComplement(BaziChart a, BaziChart b)
    {
        var aGetsFromB = a.YongShen.FavoredElements.Count(e => ElementStrength(b, e) >= 2);
        var bGetsFromA = b.YongShen.FavoredElements.Count(e => ElementStrength(a, e) >= 2);
        var total = aGetsFromB + bGetsFromA;

        return total switch
        {
            >= 3 => $"用神互补明显（双向共 {total} 项）",
            2 => "用神部分互补",
            1 => "用神单向补益",
            _ => "用神互补有限"
        };
    }

    private static int ElementStrength(BaziChart chart, string element) =>
        chart.WuXingSummary.Counts.GetValueOrDefault(element);

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

    private static int Score(
        string dayRelation, int overlap, string complement,
        IReadOnlyList<string> zhiPairs, string dayNaYin, string yongShenComplement)
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

        if (dayNaYin.Contains("相生"))
        {
            score += 6;
        }
        else if (dayNaYin.Contains("相克"))
        {
            score -= 4;
        }

        if (yongShenComplement.Contains("明显"))
        {
            score += 12;
        }
        else if (yongShenComplement.Contains("部分"))
        {
            score += 6;
        }
        else if (yongShenComplement.Contains("单向"))
        {
            score += 3;
        }

        score += zhiPairs.Count(t => t.Contains("六合")) * 5;
        score -= zhiPairs.Count(t => t.Contains("相冲")) * 4;
        return Math.Clamp(score, 0, 100);
    }

    private static string BuildSummary(
        BaziChart a, BaziChart b, string relation, string dayNaYin,
        string yongShen, int score, IReadOnlyList<string> zhiPairs)
    {
        var zhiNote = zhiPairs.Count > 0 ? $"地支见{string.Join("、", zhiPairs)}。" : "";
        return $"日主{a.DayMaster}与{b.DayMaster}，{relation}；日柱纳音{dayNaYin}；{yongShen}。合盘指数 {score}/100。{zhiNote}";
    }
}

public record PersonSummary(
    string DayMaster,
    string DayPillar,
    string DayNaYin,
    string DominantElement,
    string Strength,
    IReadOnlyList<string> FavoredElements);

public record HePanResult(
    PersonSummary PersonA,
    PersonSummary PersonB,
    string DayMasterRelation,
    string DayNaYinRelation,
    string YearNaYinRelation,
    int ElementOverlap,
    string ElementComplement,
    string YongShenComplement,
    IReadOnlyList<string> ZhiPairs,
    int Score,
    string Summary);
