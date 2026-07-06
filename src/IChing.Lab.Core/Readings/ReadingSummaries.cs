using System.Text.Json;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Rules;
using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Core.Readings;

public static class ReadingSummaries
{
    public const string Tier0Disclaimer = "Rule-based preview generated without AI interpretation.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static Tier0Preview BuildBaziTier0Preview(BaziChart chart, string? focus)
    {
        var focusText = Blank(focus, "general");
        var oneLiner =
            $"Day master {chart.DayMaster}, month pillar {chart.MonthPillar.GanZhi}, pattern {chart.YongShen.GeJu.Pattern}, strength {chart.YongShen.Strength}, yongshen {chart.YongShen.PrimaryYongShen}; focus {focusText}.";
        return new Tier0Preview(oneLiner, Tier0Disclaimer);
    }

    public static Tier0Preview BuildLiuyaoTier0Preview(LiuyaoNajiaResult chart, string? question, string? focus)
    {
        var changing = chart.Lines.Where(l => l.IsChanging).Select(l => $"line {l.Index}").ToList();
        var changeText = changing.Count == 0 ? "no moving lines" : string.Join(", ", changing) + " moving";
        var changed = chart.ChangedHexagram is null ? "" : $" changing to {chart.ChangedHexagram}";
        var oneLiner = $"{chart.OriginalHexagram}{changed}; {changeText}. Unclassified questions use the Shi line/世爻 as yongshen.";
        if (!string.IsNullOrWhiteSpace(question))
        {
            oneLiner = $"Question '{question.Trim()}': {oneLiner}";
        }
        else if (!string.IsNullOrWhiteSpace(focus))
        {
            oneLiner = $"Focus '{focus.Trim()}': {oneLiner}";
        }

        return new Tier0Preview(oneLiner, Tier0Disclaimer);
    }

    public static Tier0Preview BuildTarotTier0Preview(TarotReading reading, string? question)
    {
        var narrative = TarotNarrative.Build(reading);
        var cards = string.Join("; ", reading.Positions.Select(p =>
            $"[{p.PositionTitleZh}] {p.CardName} {(p.Reversed ? "reversed" : "upright")}: {Truncate(p.Meaning, 48)}"));
        var oneLiner = question is { Length: > 0 }
            ? $"Question '{question}': {narrative.Summary} {cards}"
            : $"{narrative.Summary} {cards}";
        return new Tier0Preview(oneLiner.Trim(), Tier0Disclaimer);
    }

    public static BaziPreview BuildBaziPreview(BaziChart chart, string? focus) =>
        new(BuildBaziTier0Preview(chart, focus).OneLiner);

    public static BaziRuleDigest BuildBaziRuleDigest(BaziChart chart, string? focus, RuleEngine? engine = null)
    {
        var rules = (engine ?? RuleEngine.Default).Run("bazi", chart, null, focus);
        var pillars = $"{chart.YearPillar.GanZhi} {chart.MonthPillar.GanZhi} {chart.DayPillar.GanZhi} {chart.HourPillar.GanZhi}";
        return new BaziRuleDigest(
            DayMaster: chart.DayMaster,
            PillarSummary: pillars,
            YongShenSummary: chart.YongShen.Summary,
            ActivePlugins: rules.ActivePlugins,
            Items: rules.Items);
    }

    public static LiuyaoRuleDigest BuildLiuyaoRuleDigest(
        LiuyaoNajiaResult chart,
        string? question,
        string? focus,
        RuleEngine? engine = null)
    {
        var shi = chart.Lines.FirstOrDefault(IsShi);
        var ying = chart.Lines.FirstOrDefault(IsYing);
        var rules = (engine ?? RuleEngine.Default).Run("liuyao", chart, question, focus);
        var yongshen = rules.Items.FirstOrDefault(i => i.PluginId == "liuyao.yongshen.keyword")?.Text
            ?? "Unclassified question: use Shi line as yongshen.";

        return new LiuyaoRuleDigest(
            ShiYaoSummary: shi is null ? "Shi line not marked." : $"line {shi.Index}: {shi.SixKin} {shi.StemBranch} holds Shi",
            YingYaoSummary: ying is null ? "Ying line not marked." : $"line {ying.Index}: {ying.SixKin} {ying.StemBranch} is Ying",
            ChangingSummaries: chart.Lines
                .Where(l => l.IsChanging)
                .Select(l => $"line {l.Index}: {l.SixKin} {l.StemBranch} moving")
                .ToList(),
            QuestionType: Blank(focus, Blank(question, "general")),
            YongShenSummary: yongshen,
            Alerts: [],
            ActivePlugins: rules.ActivePlugins,
            Items: rules.Items);
    }

    public static TarotRuleDigest BuildTarotRuleDigest(TarotReading reading, RuleEngine? engine = null)
    {
        var names = reading.Positions.Select(p => p.CardName).ToList();
        var rules = (engine ?? RuleEngine.Default).Run("tarot", reading, reading.Question, null);
        return new TarotRuleDigest(
            MajorCount: names.Count(n => !n.Contains(" of ", StringComparison.Ordinal)),
            Total: names.Count,
            Wands: CountSuit(names, "Wands"),
            Cups: CountSuit(names, "Cups"),
            Swords: CountSuit(names, "Swords"),
            Pentacles: CountSuit(names, "Pentacles"),
            ReversedCount: reading.Positions.Count(p => p.Reversed),
            ActivePlugins: rules.ActivePlugins,
            Items: rules.Items);
    }

    public static string BuildChatPrompt(string domain, string? question, string? focus, object chart, object? ruleDigest)
    {
        return $"""
        You are a careful {domain} reading assistant.
        Treat the chart, hexagram, spread, moving lines, and rule digest as computed facts. Do not invent missing cards, lines, stems, branches, or positions.
        Write concise, practical Simplified Chinese for the user.

        Question: {Blank(question, "general")}
        Focus: {Blank(focus, "general")}

        Rule digest:
        {JsonSerializer.Serialize(ruleDigest, JsonOptions)}

        Computed chart:
        {JsonSerializer.Serialize(chart, JsonOptions)}
        """;
    }

    private static bool IsShi(LiuyaoLineDetail line) =>
        ContainsAny(line.Role ?? "", "Shi", "Worldly", "世");

    private static bool IsYing(LiuyaoLineDetail line) =>
        ContainsAny(line.Role ?? "", "Ying", "应", "應");

    private static int CountSuit(IReadOnlyList<string> names, string suit) =>
        names.Count(n => n.EndsWith($"of {suit}", StringComparison.Ordinal));

    private static string Blank(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...";

    private static bool ContainsAny(string text, params string[] words) =>
        words.Any(w => text.Contains(w, StringComparison.OrdinalIgnoreCase));
}

public record Tier0Preview(string OneLiner, string Disclaimer);

public record BaziPreview(string OneLiner);

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
