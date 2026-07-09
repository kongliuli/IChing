using IChing.Lab.Core.Tarot;

namespace IChing.Tarot.App.Services;

public static class TarotReadingStats
{
    public static double CoveragePercent(TarotReading reading)
    {
        if (reading.Positions.Count == 0)
        {
            return 0;
        }

        var hits = reading.Positions.Count(p => !string.IsNullOrWhiteSpace(p.Meaning));
        return Math.Round(hits * 100d / reading.Positions.Count, 1);
    }

    public static object BuildRuleDigest(TarotReading reading)
    {
        var names = reading.Positions.Select(p => p.CardName).ToList();
        return new
        {
            majorCount = names.Count(n => !n.Contains(" of ", StringComparison.Ordinal)),
            total = names.Count,
            wands = names.Count(n => n.EndsWith("of Wands", StringComparison.Ordinal)),
            cups = names.Count(n => n.EndsWith("of Cups", StringComparison.Ordinal)),
            swords = names.Count(n => n.EndsWith("of Swords", StringComparison.Ordinal)),
            pentacles = names.Count(n => n.EndsWith("of Pentacles", StringComparison.Ordinal)),
            reversedCount = reading.Positions.Count(p => p.Reversed),
            meaningCoverage = CoveragePercent(reading)
        };
    }
}
