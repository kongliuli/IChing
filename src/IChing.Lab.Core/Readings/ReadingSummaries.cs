using System.Text.Json;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Rules;
using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Core.Readings;

public static class ReadingSummaries
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static BaziPreview BuildBaziPreview(BaziChart chart, string? focus, RuleEngine? engine = null)
    {
        var result = (engine ?? RuleEngine.Default).Run("bazi", chart, focus: focus);
        return new BaziPreview(
            $"Day pillar {chart.DayPillar.GanZhi}, month pillar {chart.MonthPillar.GanZhi}; focus: {Blank(focus, "general")}.",
            result.ActivePlugins,
            result.Items);
    }

    public static BaziRuleDigest BuildBaziRuleDigest(BaziChart chart, string? focus, RuleEngine? engine = null)
    {
        var result = (engine ?? RuleEngine.Default).Run("bazi", chart, focus: focus);
        return new BaziRuleDigest(
            chart.DayMaster,
            $"{chart.YearPillar.GanZhi} {chart.MonthPillar.GanZhi} {chart.DayPillar.GanZhi} {chart.HourPillar.GanZhi}",
            chart.YongShen.Summary,
            result.ActivePlugins,
            result.Items);
    }

    public static LiuyaoRuleDigest BuildLiuyaoRuleDigest(LiuyaoNajiaResult chart, string? question, string? focus, RuleEngine? engine = null)
    {
        var result = (engine ?? RuleEngine.Default).Run("liuyao", chart, question, focus);
        var shi = chart.Lines.FirstOrDefault(IsShi);
        var ying = chart.Lines.FirstOrDefault(IsYing);
        var yongShen = result.Items.FirstOrDefault(i => i.PluginId == "liuyao.yongshen.keyword")?.Text
            ?? "Unclassified questions use the Shi line as the subject.";

        return new LiuyaoRuleDigest(
            shi is null ? "Shi line not marked" : $"{shi.Index}: {shi.SixKin} {shi.StemBranch}",
            ying is null ? "Ying line not marked" : $"{ying.Index}: {ying.SixKin} {ying.StemBranch}",
            chart.Lines
                .Where(l => l.IsChanging)
                .Select(l => $"{l.Index}: {l.SixKin} {l.StemBranch} changes")
                .ToList(),
            Blank(focus, Blank(question, "general")),
            yongShen,
            [],
            result.ActivePlugins,
            result.Items);
    }

    public static TarotRuleDigest BuildTarotRuleDigest(TarotReading reading, RuleEngine? engine = null)
    {
        var result = (engine ?? RuleEngine.Default).Run("tarot", reading, reading.Question);
        var names = reading.Positions.Select(p => p.CardName).ToList();
        return new TarotRuleDigest(
            names.Count(n => !n.Contains(" of ", StringComparison.Ordinal)),
            names.Count,
            names.Count(n => n.EndsWith("of Wands", StringComparison.Ordinal)),
            names.Count(n => n.EndsWith("of Cups", StringComparison.Ordinal)),
            names.Count(n => n.EndsWith("of Swords", StringComparison.Ordinal)),
            names.Count(n => n.EndsWith("of Pentacles", StringComparison.Ordinal)),
            reading.Positions.Count(p => p.Reversed),
            result.ActivePlugins,
            result.Items);
    }

    public static string BuildChatPrompt(string domain, string? question, string? focus, object chart, object? ruleDigest)
    {
        var domainName = domain switch
        {
            "bazi" => "BaZi",
            "liuyao" => "Liuyao",
            "tarot" => "Tarot",
            _ => domain
        };

        return $"""
        You are a careful divination reading assistant for {domainName}.
        The chart, cards, hexagrams, line states, and facts below were computed by software.
        Do not change any computed fact. Do not invent missing cards, hexagrams, stems, branches, dates, or line states.
        Write a concise, practical Simplified Chinese reading for a friend.

        Question: {Blank(question, "general")}
        Focus: {Blank(focus, "general")}

        Rule digest:
        {JsonSerializer.Serialize(ruleDigest, JsonOptions)}

        Computed result:
        {JsonSerializer.Serialize(chart, JsonOptions)}
        """;
    }

    private static string Blank(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static bool IsShi(LiuyaoLineDetail line) =>
        line.Role?.Contains("世", StringComparison.Ordinal) == true ||
        line.Role?.Contains("涓?", StringComparison.Ordinal) == true;

    private static bool IsYing(LiuyaoLineDetail line) =>
        line.Role?.Contains("应", StringComparison.Ordinal) == true ||
        line.Role?.Contains("搴?", StringComparison.Ordinal) == true;
}

public record BaziPreview(
    string OneLiner,
    IReadOnlyList<string> ActivePlugins,
    IReadOnlyList<RuleDigestItem> Items);

public record BaziRuleDigest(
    string DayMaster,
    string PillarSummary,
    string YongShenSummary,
    IReadOnlyList<string> ActivePlugins,
    IReadOnlyList<RuleDigestItem> Items);

public record LiuyaoRuleDigest(
    string ShiYaoSummary,
    string YingYaoSummary,
    IReadOnlyList<string> ChangingSummaries,
    string QuestionType,
    string YongShenSummary,
    IReadOnlyList<string> Alerts,
    IReadOnlyList<string> ActivePlugins,
    IReadOnlyList<RuleDigestItem> Items);

public record TarotRuleDigest(
    int MajorCount,
    int Total,
    int Wands,
    int Cups,
    int Swords,
    int Pentacles,
    int ReversedCount,
    IReadOnlyList<string> ActivePlugins,
    IReadOnlyList<RuleDigestItem> Items);
