using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Rules;
using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Core.Readings;

public static class ExchangeInputBuilder
{
    public static ExchangeInput ForBazi(BaziChart chart, BaziRuleDigest digest, string? focus) =>
        new(
            Question: null,
            Focus: focus,
            ComputedFacts:
            [
                $"pillars: {chart.YearPillar.GanZhi} {chart.MonthPillar.GanZhi} {chart.DayPillar.GanZhi} {chart.HourPillar.GanZhi}",
                $"dayMaster: {chart.DayMaster}"
            ],
            RuleDigest: [digest.PillarSummary, digest.YongShenSummary],
            PluginContext: PluginContextFrom(digest.Items));

    public static ExchangeInput ForLiuyao(
        LiuyaoNajiaResult chart,
        LiuyaoRuleDigest digest,
        string? question,
        string? focus) =>
        new(
            Question: question,
            Focus: focus,
            ComputedFacts:
            [
                $"hexagram: {chart.OriginalHexagram}",
                chart.ChangedHexagram is null ? "changed: none" : $"changed: {chart.ChangedHexagram}"
            ],
            RuleDigest: [digest.ShiYaoSummary, digest.YongShenSummary],
            PluginContext: PluginContextFrom(digest.Items));

    public static ExchangeInput ForTarot(TarotReading reading, string? question) =>
        new(
            Question: question ?? reading.Question,
            Focus: null,
            ComputedFacts:
            [
                $"spread: {reading.SpreadTitleZh}",
                ..reading.Positions.Select(p =>
                    $"[{p.PositionTitleZh}] {p.CardNameZh} {(p.Reversed ? "逆位" : "正位")}")
            ],
            RuleDigest: reading.Positions.Select(p => p.Meaning).ToArray(),
            PluginContext: []);

    private static IReadOnlyList<ExchangePluginContext> PluginContextFrom(IReadOnlyList<RuleDigestItem> items) =>
        items
            .GroupBy(i => i.PluginId, StringComparer.Ordinal)
            .Select(g => new ExchangePluginContext(
                g.Key,
                g.Select(i => i.Text).ToList(),
                []))
            .ToList();
}
