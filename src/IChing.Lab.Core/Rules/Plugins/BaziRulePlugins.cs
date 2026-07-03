using IChing.Lab.Core.Bazi;

namespace IChing.Lab.Core.Rules.Plugins;

public static class BaziRulePlugins
{
    public static readonly IReadOnlyList<RulePlugin> All =
    [
        new("bazi.base.pillars", "bazi", "Pillar basics", "Day master, four pillars, and month branch.", 100, true, ctx =>
        {
            var chart = (BaziChart)ctx.Chart;
            var text = $"day master {chart.DayMaster}; pillars {chart.YearPillar.GanZhi} {chart.MonthPillar.GanZhi} {chart.DayPillar.GanZhi} {chart.HourPillar.GanZhi}; month branch {chart.MonthPillar.Zhi}";
            return [new RuleDigestItem("bazi.base.pillars", "Pillars", text, 100)];
        }),
        new("bazi.yongshen.current", "bazi", "Current yongshen", "Uses the built-in YongShenAnalyzer result.", 100, true, ctx =>
        {
            var chart = (BaziChart)ctx.Chart;
            return [new RuleDigestItem("bazi.yongshen.current", "Yongshen", chart.YongShen.Summary, 100)];
        }),
        new("bazi.wuxing.balance", "bazi", "Five-phase balance", "Highlights the most visible element count without treating count alone as fate.", 90, true, ctx =>
        {
            var chart = (BaziChart)ctx.Chart;
            var top = chart.WuXingSummary.Counts.OrderByDescending(kv => kv.Value).FirstOrDefault();
            return string.IsNullOrWhiteSpace(top.Key)
                ? []
                : [new RuleDigestItem("bazi.wuxing.balance", "Five phases", $"{top.Key} appears most often; read it with month command and yongshen, not by count alone.", 90)];
        }),
        new("bazi.flow.current", "bazi", "Current flow", "Summarizes da-yun, flow year, and selected flow month.", 80, true, ctx =>
        {
            var chart = (BaziChart)ctx.Chart;
            if (chart.FlowYear is null)
            {
                return [];
            }

            var selected = chart.FlowYear.SelectedMonth is null ? "" : $"; flow month {chart.FlowYear.SelectedMonth.GanZhi}";
            var text = $"flow year {chart.FlowYear.Year} {chart.FlowYear.GanZhi}; da-yun {chart.FlowYear.DaYunGanZhi}{selected}";
            return [new RuleDigestItem("bazi.flow.current", "Flow", text, 80)];
        })
    ];
}
