using IChing.Lab.Core.Bazi;

namespace IChing.Lab.Core.Bazi;

/// <summary>八字命盘 prompt 压缩：减小 token 占用，供 Inference 层通过 object 调用而不直接引用 <see cref="BaziChart"/>。</summary>
public static class BaziChartPromptCompactor
{
    public static object Compact(object chartJson) =>
        chartJson is BaziChart chart ? CompactChart(chart) : chartJson;

    public static object CompactChart(BaziChart chart) => new
    {
        chart.Engine,
        chart.WallClock,
        chart.TrueSolarTime,
        chart.Solar,
        chart.Lunar,
        chart.DayMaster,
        pillars = new
        {
            year = CompactPillar(chart.YearPillar),
            month = CompactPillar(chart.MonthPillar),
            day = CompactPillar(chart.DayPillar),
            hour = CompactPillar(chart.HourPillar)
        },
        chart.WuXingSummary,
        chart.Yun,
        daYun = chart.DaYun?.Take(5),
        flowYear = chart.FlowYear is null ? null : new
        {
            chart.FlowYear.Year,
            chart.FlowYear.GanZhi,
            chart.FlowYear.Age,
            chart.FlowYear.DaYunGanZhi,
            selectedMonth = chart.FlowYear.SelectedMonth is null ? null : new
            {
                chart.FlowYear.SelectedMonth.Index,
                chart.FlowYear.SelectedMonth.MonthInChinese,
                chart.FlowYear.SelectedMonth.GanZhi,
                chart.FlowYear.SelectedMonth.JieQiStart,
                chart.FlowYear.SelectedMonth.StartSolar,
                chart.FlowYear.SelectedMonth.JieQiEnd,
                chart.FlowYear.SelectedMonth.EndSolar
            },
            chart.FlowYear.SelectedDay
        },
        yongShen = new
        {
            chart.YongShen.Strength,
            geJu = chart.YongShen.GeJu.Pattern,
            geJuBreak = chart.YongShen.GeJu.Break?.Summary,
            chart.YongShen.PrimaryYongShen,
            chart.YongShen.SecondaryYongShen,
            chart.YongShen.FavoredElements,
            chart.YongShen.Summary
        }
    };

    private static object CompactPillar(BaziPillar p) => new
    {
        p.GanZhi,
        p.Gan,
        p.Zhi,
        p.WuXing,
        p.NaYin,
        p.ShiShenGan,
        p.HideGan
    };
}
