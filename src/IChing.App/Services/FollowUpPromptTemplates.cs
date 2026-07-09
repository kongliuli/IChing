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

    public static (string SystemPrompt, string Context) Bazi(
        BaziChart chart,
        BaziRuleDigest digest,
        string? focus,
        string interpretation) =>
        FollowUpPromptBuilder.Build("bazi", BaziInput(chart, digest, focus), null, interpretation);

    public static (string SystemPrompt, string Context) Liuyao(
        LiuyaoNajiaResult chart,
        LiuyaoRuleDigest digest,
        string? question,
        string? focus,
        string interpretation) =>
        FollowUpPromptBuilder.Build("liuyao", LiuyaoInput(chart, digest, question, focus), null, interpretation);
}
