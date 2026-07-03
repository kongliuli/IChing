using Lunar;

namespace IChing.Lab.Core.Bazi;

/// <summary>ponytail: 十二节令定月界，起止含首日、不含下一节当日。</summary>
public static class FlowJieQiHelper
{
    private static readonly string[] JieOrder =
    [
        "立春", "惊蛰", "清明", "立夏", "芒种", "小暑",
        "立秋", "白露", "寒露", "立冬", "大雪", "小寒"
    ];

    public static FlowMonthBoundary GetBoundary(int year, int monthIndex)
    {
        var table = Solar.FromYmdHms(year, 6, 1, 0, 0, 0).Lunar.JieQiTable;
        var startName = JieOrder[monthIndex];
        var endName = JieOrder[(monthIndex + 1) % 12];

        var start = ResolveJieDate(table, startName, year);
        var endExclusive = ResolveJieDate(table, endName, year);
        if (endExclusive <= start)
        {
            endExclusive = ResolveJieDate(table, endName, year + 1);
        }

        var endInclusive = endExclusive.AddDays(-1);
        return new FlowMonthBoundary(startName, endName, start, endInclusive);
    }

    public static IReadOnlyList<FlowDayInfo> ListDaysInBoundary(FlowMonthBoundary boundary)
    {
        var days = new List<FlowDayInfo>();
        for (var d = boundary.Start; d <= boundary.End; d = d.AddDays(1))
        {
            var info = FlowDayHelper.GetDay(d.Year, d.Month, d.Day);
            if (info is not null)
            {
                days.Add(info);
            }
        }

        return days;
    }

    public static FlowMonthInfo EnrichMonth(int year, FlowMonthInfo month, bool includeDays)
    {
        var boundary = GetBoundary(year, month.Index);
        IReadOnlyList<FlowDayInfo>? days = includeDays
            ? ListDaysInBoundary(boundary)
            : null;

        return month with
        {
            JieQiStart = boundary.StartJieQi,
            StartSolar = boundary.Start.ToString("yyyy-MM-dd"),
            JieQiEnd = boundary.EndJieQi,
            EndSolar = boundary.End.ToString("yyyy-MM-dd"),
            FlowDays = days
        };
    }

    private static DateTime ResolveJieDate(IReadOnlyDictionary<string, Solar> table, string jieName, int nearYear)
    {
        if (table.TryGetValue(jieName, out var solar))
        {
            return ToDate(solar);
        }

        var pinyinKey = jieName switch
        {
            "立春" => "LI_CHUN",
            "惊蛰" => "JING_ZHE",
            "清明" => "QING_MING",
            "立夏" => "LI_XIA",
            "芒种" => "MANG_ZHONG",
            "小暑" => "XIAO_SHU",
            "立秋" => "LI_QIU",
            "白露" => "BAI_LU",
            "寒露" => "HAN_LU",
            "立冬" => "LI_DONG",
            "大雪" => "DA_XUE",
            "小寒" => "XIAO_HAN",
            _ => jieName
        };

        if (table.TryGetValue(pinyinKey, out solar))
        {
            return ToDate(solar);
        }

        throw new InvalidOperationException($"jie qi not found: {jieName} near {nearYear}");
    }

    private static DateTime ToDate(Solar solar) =>
        new(solar.Year, solar.Month, solar.Day);
}

public record FlowMonthBoundary(
    string StartJieQi,
    string EndJieQi,
    DateTime Start,
    DateTime End);
