using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Engines.Tarot;

/// <summary>
/// 用 Deckaura 78 牌 12 维数据 enrich 内置抽牌结果的牌义文本（小阿卡纳逐张替换模板拼接）。
/// </summary>
public static class TarotReadingEnricher
{
    public static TarotReading EnrichWithDeckaura(TarotReading reading)
    {
        var positions = reading.Positions.Select(EnrichPosition).ToList();
        return reading with { Positions = positions };
    }

    private static TarotPositionReading EnrichPosition(TarotPositionReading position)
    {
        var card = TarotDeckData.FindByNameIgnoreCase(position.CardName);
        if (card is null)
        {
            return position;
        }

        var meaning = position.Reversed ? card.Reversed : card.Upright;
        return position with { Meaning = meaning };
    }

    /// <summary>Deckaura 牌义命中率（0–100）。</summary>
    public static double DeckauraCoveragePercent(TarotReading reading)
    {
        if (reading.Positions.Count == 0)
        {
            return 0;
        }

        var hits = reading.Positions.Count(p => TarotDeckData.FindByNameIgnoreCase(p.CardName) is not null);
        return Math.Round(hits * 100d / reading.Positions.Count, 1);
    }

    /// <summary>为 Prompt 规则摘要附加 Deckaura 12 维统计。</summary>
    public static object BuildEnrichedRuleDigest(TarotReading reading)
    {
        var names = reading.Positions.Select(p => p.CardName).ToList();
        var deckauraHits = reading.Positions.Count(p => TarotDeckData.FindByNameIgnoreCase(p.CardName) is not null);
        return new
        {
            majorCount = names.Count(n => !n.Contains(" of ", StringComparison.Ordinal)),
            total = names.Count,
            wands = names.Count(n => n.EndsWith("of Wands", StringComparison.Ordinal)),
            cups = names.Count(n => n.EndsWith("of Cups", StringComparison.Ordinal)),
            swords = names.Count(n => n.EndsWith("of Swords", StringComparison.Ordinal)),
            pentacles = names.Count(n => n.EndsWith("of Pentacles", StringComparison.Ordinal)),
            reversedCount = reading.Positions.Count(p => p.Reversed),
            deckauraEnriched = deckauraHits,
            deckauraCoverage = reading.Positions.Count == 0
                ? 0d
                : Math.Round(deckauraHits * 100d / reading.Positions.Count, 1)
        };
    }
}
