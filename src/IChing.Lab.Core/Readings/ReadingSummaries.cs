using System.Text.Json;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Core.Readings;

public static class ReadingSummaries
{
    public const string Tier0Disclaimer = "概览由规则模板生成，非 AI 解读";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static Tier0Preview BuildBaziTier0Preview(BaziChart chart, string? focus)
    {
        var focusText = Blank(focus, "综合");
        var pattern = chart.YongShen.GeJu.Pattern;
        var strength = chart.YongShen.Strength;
        var yong = chart.YongShen.PrimaryYongShen;
        var oneLiner =
            $"日主{chart.DayMaster}，{chart.MonthPillar.GanZhi}月建，格局「{pattern}」、身{strength}，用神倾向{yong}；关注点「{focusText}」。";
        return new Tier0Preview(oneLiner, Tier0Disclaimer);
    }

    public static Tier0Preview BuildLiuyaoTier0Preview(LiuyaoNajiaResult chart, string? question, string? focus)
    {
        var changing = chart.Lines.Where(l => l.IsChanging).Select(l => $"{l.Index}爻").ToList();
        var changeText = changing.Count == 0
            ? "无动爻"
            : string.Join("、", changing) + "动";
        var changed = chart.ChangedHexagram is null ? "" : $" 之 {chart.ChangedHexagram}";
        var oneLiner = $"{chart.OriginalHexagram}{changed}；{changeText}。未分类问事默认以世爻为用神。";
        if (!string.IsNullOrWhiteSpace(question))
        {
            oneLiner = $"问「{question.Trim()}」：{oneLiner}";
        }
        else if (!string.IsNullOrWhiteSpace(focus))
        {
            oneLiner = $"焦点「{focus.Trim()}」：{oneLiner}";
        }

        return new Tier0Preview(oneLiner, Tier0Disclaimer);
    }

    public static Tier0Preview BuildTarotTier0Preview(TarotReading reading, string? question)
    {
        var narrative = TarotNarrative.Build(reading);
        var cards = string.Join("；", reading.Positions.Select(p =>
            $"[{p.PositionTitleZh}] {p.CardNameZh}{(p.Reversed ? "逆位" : "正位")}：{Truncate(p.Meaning, 24)}"));
        var oneLiner = question is { Length: > 0 }
            ? $"【{question}】{narrative.Summary} {cards}"
            : $"{narrative.Summary} {cards}";
        return new Tier0Preview(oneLiner.Trim(), Tier0Disclaimer);
    }

    public static BaziPreview BuildBaziPreview(BaziChart chart, string? focus) =>
        new(BuildBaziTier0Preview(chart, focus).OneLiner);

    public static LiuyaoRuleDigest BuildLiuyaoRuleDigest(LiuyaoNajiaResult chart, string? question, string? focus)
    {
        var shi = chart.Lines.FirstOrDefault(l => l.Role?.Contains("世", StringComparison.Ordinal) == true);
        var ying = chart.Lines.FirstOrDefault(l => l.Role?.Contains("应", StringComparison.Ordinal) == true);

        return new LiuyaoRuleDigest(
            ShiYaoSummary: shi is null ? "未标出世爻" : $"{shi.Index}爻{shi.SixKin}{shi.StemBranch}持世",
            YingYaoSummary: ying is null ? "未标出应爻" : $"{ying.Index}爻{ying.SixKin}{ying.StemBranch}为应",
            ChangingSummaries: chart.Lines
                .Where(l => l.IsChanging)
                .Select(l => $"{l.Index}爻{l.SixKin}{l.StemBranch}动")
                .ToList(),
            QuestionType: Blank(focus, Blank(question, "综合")),
            YongShenSummary: "未分类问事默认以世爻为用神",
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
            "bazi" => "八字",
            "liuyao" => "六爻",
            "tarot" => "塔罗",
            _ => domain
        };

        return $"""
        你是谨慎的{domainName}解读助手。
        下列命盘、牌阵、卦象与动爻状态均由软件计算，请勿修改任何计算结果，勿编造未出现的牌、卦、干支或爻位。
        用简体中文、简洁实用、像朋友聊天一样输出解读。

        问题：{Blank(question, "综合")}
        焦点：{Blank(focus, "综合")}

        规则摘要：
        {JsonSerializer.Serialize(ruleDigest, JsonOptions)}

        计算结果：
        {JsonSerializer.Serialize(chart, JsonOptions)}
        """;
    }

    private static string Blank(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}

public record Tier0Preview(string OneLiner, string Disclaimer);

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
