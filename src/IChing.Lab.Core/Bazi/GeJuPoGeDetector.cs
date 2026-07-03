namespace IChing.Lab.Core.Bazi;

/// <summary>ponytail: 透干/冲月令/混杂等启发式破格，Lab 级非流派精算。</summary>
public static class GeJuPoGeDetector
{
    private static readonly HashSet<string> GuanSha = ["正官", "七杀"];
    private static readonly HashSet<string> Yin = ["正印", "偏印"];
    private static readonly HashSet<string> ShiShang = ["食神", "伤官"];
    private static readonly HashSet<string> BiJie = ["比肩", "劫财"];
    private static readonly HashSet<string> Cai = ["正财", "偏财"];

    public static GeJuBreakResult Detect(BaziChart chart, GeJuInfo geJu, string strength)
    {
        var breaks = new List<string>();

        switch (geJu.Category)
        {
            case "从格":
                DetectCongBreaks(chart, strength, breaks);
                break;
            case "专旺":
                DetectZhuanWangBreaks(chart, breaks);
                break;
            default:
                DetectRegularBreaks(chart, geJu.Pattern, breaks);
                break;
        }

        DetectCommonBreaks(chart, breaks);

        var intact = breaks.Count == 0;
        return new GeJuBreakResult(
            intact,
            breaks,
            intact ? "格局成立" : $"破格：{string.Join("；", breaks)}");
    }

    private static void DetectRegularBreaks(BaziChart chart, string pattern, List<string> breaks)
    {
        switch (pattern)
        {
            case "正官格":
                if (HasStemShiShen(chart, "伤官"))
                {
                    breaks.Add("伤官见官");
                }

                if (HasStemShiShen(chart, "七杀"))
                {
                    breaks.Add("官杀混杂");
                }

                break;
            case "七杀格":
                if (HasStemShiShen(chart, "正官"))
                {
                    breaks.Add("官杀混杂");
                }

                if (!HasAnyShiShen(chart, "食神", "正印", "偏印"))
                {
                    breaks.Add("七杀无制");
                }

                break;
            case "财格":
                if (CountStemShiShen(chart, BiJie) >= 2)
                {
                    breaks.Add("比劫争财");
                }

                break;
            case "印格":
                if (HasStemShiShen(chart, "正财") || HasStemShiShen(chart, "偏财"))
                {
                    breaks.Add("财星破印");
                }

                break;
            case "食神格":
                if (HasStemShiShen(chart, "偏印"))
                {
                    breaks.Add("枭神夺食");
                }

                break;
            case "伤官格":
                if (HasStemShiShen(chart, "正官"))
                {
                    breaks.Add("伤官见官");
                }

                break;
        }
    }

    private static void DetectCongBreaks(BaziChart chart, string strength, List<string> breaks)
    {
        if (strength.Contains("强"))
        {
            breaks.Add("从格见日主得势");
        }

        if (HasStemShiShen(chart, "正印") || HasStemShiShen(chart, "偏印"))
        {
            if (CountStemShiShen(chart, Yin) >= 2)
            {
                breaks.Add("从格见印星生扶");
            }
        }

        if (HasStemShiShen(chart, "比肩") || HasStemShiShen(chart, "劫财"))
        {
            breaks.Add("从格见比劫帮身");
        }
    }

    private static void DetectZhuanWangBreaks(BaziChart chart, List<string> breaks)
    {
        if (HasStemShiShen(chart, "正官") || HasStemShiShen(chart, "七杀"))
        {
            breaks.Add("专旺格见官杀");
        }
    }

    private static void DetectCommonBreaks(BaziChart chart, List<string> breaks)
    {
        if (IsMonthClashed(chart))
        {
            breaks.Add("月令逢冲");
        }
    }

    private static bool IsMonthClashed(BaziChart chart)
    {
        var monthZhi = chart.MonthPillar.Zhi;
        foreach (var zhi in new[] { chart.YearPillar.Zhi, chart.DayPillar.Zhi, chart.HourPillar.Zhi })
        {
            if (IsChong(monthZhi, zhi))
            {
                return true;
            }
        }

        return false;
    }

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

    private static bool HasStemShiShen(BaziChart chart, string shiShen) =>
        chart.YearPillar.ShiShenGan == shiShen ||
        chart.MonthPillar.ShiShenGan == shiShen ||
        chart.HourPillar.ShiShenGan == shiShen;

    private static int CountStemShiShen(BaziChart chart, HashSet<string> set) =>
        new[] { chart.YearPillar.ShiShenGan, chart.MonthPillar.ShiShenGan, chart.HourPillar.ShiShenGan }
            .Count(s => set.Contains(s));

    private static bool HasAnyShiShen(BaziChart chart, params string[] names)
    {
        foreach (var pillar in new[] { chart.YearPillar, chart.MonthPillar, chart.HourPillar })
        {
            if (names.Contains(pillar.ShiShenGan))
            {
                return true;
            }
        }

        return false;
    }
}

public record GeJuBreakResult(
    bool IsIntact,
    IReadOnlyList<string> Breaks,
    string Summary);
