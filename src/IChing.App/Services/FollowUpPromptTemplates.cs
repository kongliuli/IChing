using System.Text.Json;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Readings;

namespace IChing.App.Services;

public static class FollowUpPromptTemplates
{
    private static ExchangeInput BaziInput(BaziChart chart, BaziRuleDigest digest, string? focus) =>
        new(
            Question: null,
            Focus: focus,
            ComputedFacts:
            [
                $"pillars: {chart.YearPillar.GanZhi} {chart.MonthPillar.GanZhi} {chart.DayPillar.GanZhi} {chart.HourPillar.GanZhi}",
                $"dayMaster: {chart.DayMaster}"
            ],
            RuleDigest: [digest.PillarSummary, digest.YongShenSummary],
            PluginContext: []);

    private static ExchangeInput LiuyaoInput(
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
            PluginContext: []);

    public static ExchangeInput BaziExchangeInput(BaziChart chart, BaziRuleDigest digest, string? focus) =>
        BaziInput(chart, digest, focus);

    public static ExchangeInput LiuyaoExchangeInput(
        LiuyaoNajiaResult chart,
        LiuyaoRuleDigest digest,
        string? question,
        string? focus) =>
        LiuyaoInput(chart, digest, question, focus);
}
