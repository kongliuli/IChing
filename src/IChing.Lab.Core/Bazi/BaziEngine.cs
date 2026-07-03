using Lunar;

namespace IChing.Lab.Core.Bazi;

public static class BaziEngine
{
    public static BaziChart Calculate(BaziInput input)
    {
        var longitude = input.Longitude;
        if (longitude is null && input.City is not null && CityLookup.TryGetLongitude(input.City, out var lon))
        {
            longitude = lon;
        }

        var wallClock = new DateTime(input.Year, input.Month, input.Day, input.Hour, input.Minute, input.Second);
        var corrected = longitude is null
            ? wallClock
            : TrueSolarTime.Apply(wallClock, longitude.Value);

        var solar = Solar.FromYmdHms(
            corrected.Year, corrected.Month, corrected.Day,
            corrected.Hour, corrected.Minute, corrected.Second);
        var lunar = solar.Lunar;
        var eight = lunar.EightChar;

        IReadOnlyList<DaYunPeriod>? daYun = null;
        YunInfo? yunInfo = null;
        FlowYearInfo? flowYear = null;
        if (input.Gender is not null)
        {
            var yun = eight.GetYun(input.Gender.Value, input.Sect);
            yunInfo = new YunInfo(
                yun.StartYear, yun.StartMonth, yun.StartDay, yun.StartHour,
                yun.Forward, yun.StartSolar.ToString());

            var periods = yun.GetDaYun(10);
            daYun = periods
                .Select(d => new DaYunPeriod(
                    d.Index, d.GanZhi, d.StartYear, d.EndYear, d.StartAge, d.EndAge))
                .ToList();

            if (input.FlowYear is int fy)
            {
                flowYear = ResolveFlowYear(periods, fy, input.FlowMonth, input.FlowCalendarMonth, input.FlowDay);
            }
        }

        var chart = new BaziChart(
            Engine: "lunar-csharp-1.6.8",
            WallClock: wallClock.ToString("yyyy-MM-dd HH:mm:ss"),
            TrueSolarTime: longitude is null ? null : corrected.ToString("yyyy-MM-dd HH:mm:ss"),
            Longitude: longitude,
            City: input.City,
            Solar: solar.ToString(),
            Lunar: lunar.ToString(),
            DayMaster: eight.DayGan,
            YearPillar: MapPillar(eight.Year, eight.YearGan, eight.YearZhi,
                eight.YearHideGan, eight.YearShiShenGan, eight.YearShiShenZhi,
                eight.YearWuXing, eight.YearNaYin),
            MonthPillar: MapPillar(eight.Month, eight.MonthGan, eight.MonthZhi,
                eight.MonthHideGan, eight.MonthShiShenGan, eight.MonthShiShenZhi,
                eight.MonthWuXing, eight.MonthNaYin),
            DayPillar: MapPillar(eight.Day, eight.DayGan, eight.DayZhi,
                eight.DayHideGan, eight.DayShiShenGan, eight.DayShiShenZhi,
                eight.DayWuXing, eight.DayNaYin),
            HourPillar: MapPillar(eight.Time, eight.TimeGan, eight.TimeZhi,
                eight.TimeHideGan, eight.TimeShiShenGan, eight.TimeShiShenZhi,
                eight.TimeWuXing, eight.TimeNaYin),
            WuXingSummary: SummarizeWuXing(eight),
            Yun: yunInfo,
            DaYun: daYun,
            FlowYear: flowYear,
            YongShen: null!);

        return chart with { YongShen = YongShenAnalyzer.Analyze(chart) };
    }

    private static BaziPillar MapPillar(
        string ganZhi, string gan, string zhi,
        IList<string> hideGan, string shiShenGan, IList<string> shiShenZhi,
        string wuXing, string naYin) =>
        new(ganZhi, gan, zhi, hideGan.ToList(), shiShenGan, shiShenZhi.ToList(), wuXing, naYin);

    private static WuXingSummary SummarizeWuXing(Lunar.EightChar.EightChar eight)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var wx in new[] { eight.YearWuXing, eight.MonthWuXing, eight.DayWuXing, eight.TimeWuXing })
        {
            counts.TryGetValue(wx, out var n);
            counts[wx] = n + 1;
        }

        var dominant = counts.OrderByDescending(kv => kv.Value).First().Key;
        return new WuXingSummary(counts, dominant);
    }

    private static FlowYearInfo? ResolveFlowYear(
        Lunar.EightChar.DaYun[] periods, int year, int? flowMonth,
        int? flowCalendarMonth, int? flowDay)
    {
        foreach (var period in periods)
        {
            if (year < period.StartYear || year > period.EndYear)
            {
                continue;
            }

            var liuNian = period.GetLiuNian(10);
            var match = liuNian.FirstOrDefault(l => l.Year == year);
            if (match is null)
            {
                continue;
            }

            var months = match.GetLiuYue()
                .Select(m => new FlowMonthInfo(
                    m.Index, m.MonthInChinese, m.GanZhi, m.Xun, m.XunKong))
                .ToList();

            FlowMonthInfo? selected = null;
            if (flowMonth is int fm)
            {
                selected = months.FirstOrDefault(m => m.Index == fm - 1)
                           ?? months.FirstOrDefault(m => m.Index == fm);
            }

            var xiaoYun = period.GetXiaoYun(10)
                .FirstOrDefault(x => x.Year == year);
            XiaoYunInfo? xiaoYunInfo = xiaoYun is null
                ? null
                : new XiaoYunInfo(xiaoYun.Year, xiaoYun.GanZhi, xiaoYun.Age, xiaoYun.Xun, xiaoYun.XunKong);

            IReadOnlyList<FlowDayInfo>? flowDays = null;
            FlowDayInfo? selectedDay = null;
            if (flowCalendarMonth is int cm)
            {
                flowDays = FlowDayHelper.ListDaysInMonth(year, cm);
                if (flowDay is int fd)
                {
                    selectedDay = FlowDayHelper.GetDay(year, cm, fd);
                }
            }

            return new FlowYearInfo(
                year,
                match.GanZhi,
                match.Age,
                period.GanZhi,
                period.Index,
                match.Xun,
                match.XunKong,
                months,
                selected,
                xiaoYunInfo,
                flowDays,
                selectedDay);
        }

        return null;
    }
}

public record BaziInput(
    int Year, int Month, int Day, int Hour,
    int Minute = 0, int Second = 0,
    double? Longitude = null,
    string? City = null,
    int? Gender = null,
    int Sect = 1,
    int? FlowYear = null,
    int? FlowMonth = null,
    int? FlowCalendarMonth = null,
    int? FlowDay = null);

public record YunInfo(
    int StartYear, int StartMonth, int StartDay, int StartHour,
    bool Forward, string StartSolar);

public record DaYunPeriod(
    int Index, string GanZhi, int StartYear, int EndYear, int StartAge, int EndAge);

public record BaziPillar(
    string GanZhi,
    string Gan,
    string Zhi,
    IReadOnlyList<string> HideGan,
    string ShiShenGan,
    IReadOnlyList<string> ShiShenZhi,
    string WuXing,
    string NaYin);

public record WuXingSummary(
    IReadOnlyDictionary<string, int> Counts,
    string Dominant);

public record FlowMonthInfo(
    int Index,
    string MonthInChinese,
    string GanZhi,
    string Xun,
    string XunKong);

public record XiaoYunInfo(
    int Year,
    string GanZhi,
    int Age,
    string Xun,
    string XunKong);

public record FlowYearInfo(
    int Year,
    string GanZhi,
    int Age,
    string DaYunGanZhi,
    int DaYunIndex,
    string Xun,
    string XunKong,
    IReadOnlyList<FlowMonthInfo> Months,
    FlowMonthInfo? SelectedMonth,
    XiaoYunInfo? XiaoYun,
    IReadOnlyList<FlowDayInfo>? FlowDays,
    FlowDayInfo? SelectedDay);

public record BaziChart(
    string Engine,
    string WallClock,
    string? TrueSolarTime,
    double? Longitude,
    string? City,
    string Solar,
    string Lunar,
    string DayMaster,
    BaziPillar YearPillar,
    BaziPillar MonthPillar,
    BaziPillar DayPillar,
    BaziPillar HourPillar,
    WuXingSummary WuXingSummary,
    YunInfo? Yun,
    IReadOnlyList<DaYunPeriod>? DaYun,
    FlowYearInfo? FlowYear,
    YongShenProfile YongShen);
