using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Tarot;
using System.Text;

namespace IChing.Client.Shared.Providers;

/// <summary>
/// 加厚规则解读：排盘面 / 牌阵原始牌义与爻辞级摘要（无 LLM）。
/// </summary>
public static class RichRuleReading
{
    public static string Build(
        string domain,
        object? chart,
        object? ruleDigest,
        string? question,
        string? focus) =>
        domain.ToLowerInvariant() switch
        {
            "bazi" when chart is BaziChart bazi => BuildBazi(bazi, ruleDigest as BaziRuleDigest, focus),
            "liuyao" when chart is LiuyaoNajiaResult liuyao => BuildLiuyao(liuyao, ruleDigest as LiuyaoRuleDigest, question, focus),
            "tarot" when chart is TarotReading tarot => BuildTarot(tarot, ruleDigest as TarotRuleDigest, question),
            _ => Fallback(domain, chart, ruleDigest, question, focus)
        };

    private static string BuildBazi(BaziChart chart, BaziRuleDigest? digest, string? focus)
    {
        var sb = new StringBuilder();
        sb.AppendLine("【八字排盘】");
        sb.AppendLine($"四柱：{chart.YearPillar.GanZhi} {chart.MonthPillar.GanZhi} {chart.DayPillar.GanZhi} {chart.HourPillar.GanZhi}");
        sb.AppendLine($"日主：{chart.DayMaster} · 格局：{chart.YongShen.GeJu.Pattern} · 强弱：{chart.YongShen.Strength}");
        sb.AppendLine($"用神：{chart.YongShen.PrimaryYongShen}（{chart.YongShen.Summary}）");
        if (!string.IsNullOrWhiteSpace(focus))
        {
            sb.AppendLine($"关注：{focus}");
        }

        if (digest is not null)
        {
            sb.AppendLine();
            sb.AppendLine("【规则摘要】");
            foreach (var item in digest.Items.Take(8))
            {
                sb.AppendLine($"· {item.Text}");
            }
        }

        if (chart.DaYun is { Count: > 0 })
        {
            sb.AppendLine();
            sb.AppendLine("【大运】");
            foreach (var dy in chart.DaYun.Take(4))
            {
                sb.AppendLine($"· {dy.StartAge}岁起 {dy.GanZhi}");
            }
        }

        sb.AppendLine();
        sb.AppendLine(ReadingSummaries.Tier0Disclaimer);
        return sb.ToString().Trim();
    }

    private static string BuildLiuyao(
        LiuyaoNajiaResult chart,
        LiuyaoRuleDigest? digest,
        string? question,
        string? focus)
    {
        var sb = new StringBuilder();
        sb.AppendLine("【六爻卦象】");
        sb.AppendLine($"本卦：{chart.OriginalHexagram}");
        if (!string.IsNullOrWhiteSpace(chart.ChangedHexagram))
        {
            sb.AppendLine($"变卦：{chart.ChangedHexagram}");
        }

        if (!string.IsNullOrWhiteSpace(question))
        {
            sb.AppendLine($"问事：{question}");
        }
        else if (!string.IsNullOrWhiteSpace(focus))
        {
            sb.AppendLine($"关注：{focus}");
        }

        sb.AppendLine();
        sb.AppendLine("【爻位】");
        foreach (var line in chart.Lines.OrderBy(l => l.Index))
        {
            var mark = line.IsChanging ? "（动）" : "";
            var role = string.IsNullOrWhiteSpace(line.Role) ? "" : $" [{line.Role}]";
            sb.AppendLine($"第{line.Index}爻 {line.SixKin} {line.StemBranch}{role}{mark}");
        }

        if (digest is not null)
        {
            sb.AppendLine();
            sb.AppendLine("【规则摘要】");
            sb.AppendLine(digest.ShiYaoSummary);
            sb.AppendLine(digest.YingYaoSummary);
            sb.AppendLine(digest.YongShenSummary);
            foreach (var c in digest.ChangingSummaries)
            {
                sb.AppendLine(c);
            }
        }

        sb.AppendLine();
        sb.AppendLine(ReadingSummaries.Tier0Disclaimer);
        return sb.ToString().Trim();
    }

    private static string BuildTarot(TarotReading reading, TarotRuleDigest? digest, string? question)
    {
        var sb = new StringBuilder();
        sb.AppendLine("【塔罗牌阵】");
        sb.AppendLine($"牌阵：{reading.SpreadTitleZh}");
        if (!string.IsNullOrWhiteSpace(question))
        {
            sb.AppendLine($"问题：{question}");
        }

        sb.AppendLine();
        sb.AppendLine("【牌位解读】");
        foreach (var p in reading.Positions)
        {
            var orient = p.Reversed ? "逆位" : "正位";
            sb.AppendLine($"· [{p.PositionTitleZh}] {p.CardName}（{orient}）");
            if (!string.IsNullOrWhiteSpace(p.PositionContext))
            {
                sb.AppendLine($"  位置含义：{p.PositionContext}");
            }

            if (!string.IsNullOrWhiteSpace(p.Meaning))
            {
                sb.AppendLine($"  牌义：{p.Meaning}");
            }
        }

        if (digest is not null)
        {
            sb.AppendLine();
            sb.AppendLine("【牌阵统计】");
            sb.AppendLine(
                $"大阿卡纳 {digest.MajorCount}/{digest.Total} · 逆位 {digest.ReversedCount} · " +
                $"火{digest.Wands}/水{digest.Cups}/风{digest.Swords}/土{digest.Pentacles}");
        }

        var narrative = TarotNarrative.Build(reading);
        if (!string.IsNullOrWhiteSpace(narrative.Summary))
        {
            sb.AppendLine();
            sb.AppendLine("【概览】");
            sb.AppendLine(narrative.Summary);
        }

        sb.AppendLine();
        sb.AppendLine(ReadingSummaries.Tier0Disclaimer);
        return sb.ToString().Trim();
    }

    private static string Fallback(
        string domain,
        object? chart,
        object? ruleDigest,
        string? question,
        string? focus)
    {
        var preview = domain.ToLowerInvariant() switch
        {
            "bazi" when chart is BaziChart b => ReadingSummaries.BuildBaziTier0Preview(b, focus),
            "liuyao" when chart is LiuyaoNajiaResult l => ReadingSummaries.BuildLiuyaoTier0Preview(l, question, focus),
            "tarot" when chart is TarotReading t => ReadingSummaries.BuildTarotTier0Preview(t, question),
            _ => new Tier0Preview($"[{domain}] 规则预览不可用", ReadingSummaries.Tier0Disclaimer)
        };
        return $"{preview.OneLiner}\n\n{preview.Disclaimer}";
    }
}
