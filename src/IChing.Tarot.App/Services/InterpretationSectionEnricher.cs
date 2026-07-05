using IChing.Lab.Core.Tarot;
using IChing.Tarot.App.Models;

namespace IChing.Tarot.App.Services;

public static class InterpretationSectionEnricher
{
    public static IReadOnlyList<InterpretationSectionItem> Enrich(
        IReadOnlyList<InterpretationSectionItem> sections,
        TarotReading reading)
    {
        return sections
            .Select(s => new InterpretationSectionItem
            {
                Title = s.Title,
                Body = s.Body,
                IsSubsection = s.IsSubsection,
                Band = s.Band,
                Accent = s.Accent,
                CardSource = MatchCardSource(s.Title, reading)
            })
            .ToList();
    }

    private static string? MatchCardSource(string title, TarotReading reading)
    {
        foreach (var p in reading.Positions)
        {
            if (title.Contains(p.PositionTitleZh, StringComparison.Ordinal) ||
                title.Contains(p.CardNameZh, StringComparison.Ordinal) ||
                title.Contains(p.CardName, StringComparison.OrdinalIgnoreCase))
            {
                return FormatPosition(p);
            }
        }

        return null;
    }

    private static string FormatPosition(TarotPositionReading p) =>
        $"[{p.PositionTitleZh}]\n{p.CardNameZh}（{p.CardName}）· {(p.Reversed ? "逆位" : "正位")}\n\n{p.Meaning}";
}
