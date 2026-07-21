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
}
