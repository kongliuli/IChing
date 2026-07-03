using System.Text.Json;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Core.Readings;

public static class ReadingSummaries
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static BaziPreview BuildBaziPreview(BaziChart chart, string? focus) =>
        new($"Day pillar {chart.DayPillar.GanZhi}, month pillar {chart.MonthPillar.GanZhi}; focus: {Blank(focus, "general")}.");

    public static LiuyaoRuleDigest BuildLiuyaoRuleDigest(LiuyaoNajiaResult chart, string? question, string? focus)
    {
        var shi = chart.Lines.FirstOrDefault(l => l.Role?.Contains("世", StringComparison.Ordinal) == true);
        var ying = chart.Lines.FirstOrDefault(l => l.Role?.Contains("应", StringComparison.Ordinal) == true);

        return new LiuyaoRuleDigest(
            ShiYaoSummary: shi is null ? "Shi line not marked" : $"{shi.Index}: {shi.SixKin} {shi.StemBranch}",
            YingYaoSummary: ying is null ? "Ying line not marked" : $"{ying.Index}: {ying.SixKin} {ying.StemBranch}",
            ChangingSummaries: chart.Lines
                .Where(l => l.IsChanging)
                .Select(l => $"{l.Index}: {l.SixKin} {l.StemBranch} changes")
                .ToList(),
            QuestionType: Blank(focus, Blank(question, "general")),
            YongShenSummary: "Unclassified questions use the Shi line as the subject.",
            Alerts: []);
    }

    public static TarotRuleDigest BuildTarotRuleDigest(TarotReading reading)
    {
        var names = reading.Positions.Select(p => p.CardName).ToList();
        return new TarotRuleDigest(
            MajorCount: names.Count(n => !n.Contains(" of ", StringComparison.Ordinal)),
            Total: names.Count,
            Wands: names.Count(n => n.EndsWith("of Wands", StringComparison.Ordinal)),
            Cups: names.Count(n => n.EndsWith("of Cups", StringComparison.Ordinal)),
            Swords: names.Count(n => n.EndsWith("of Swords", StringComparison.Ordinal)),
            Pentacles: names.Count(n => n.EndsWith("of Pentacles", StringComparison.Ordinal)),
            ReversedCount: reading.Positions.Count(p => p.Reversed));
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
}

public record BaziPreview(string OneLiner);

public record LiuyaoRuleDigest(
    string ShiYaoSummary,
    string YingYaoSummary,
    IReadOnlyList<string> ChangingSummaries,
    string QuestionType,
    string YongShenSummary,
    IReadOnlyList<string> Alerts);

public record TarotRuleDigest(
    int MajorCount,
    int Total,
    int Wands,
    int Cups,
    int Swords,
    int Pentacles,
    int ReversedCount);
