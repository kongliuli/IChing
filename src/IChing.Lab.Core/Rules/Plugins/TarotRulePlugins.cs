using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Core.Rules.Plugins;

public static class TarotRulePlugins
{
    public static readonly IReadOnlyList<RulePlugin> All =
    [
        new("tarot.base.spread", "tarot", "Spread basics", "Spread, positions, card names, and upright/reversed state.", 100, true, ctx =>
        {
            var reading = (TarotReading)ctx.Chart;
            var text = $"{reading.SpreadTitle}, {reading.Positions.Count} cards: " +
                       string.Join("; ", reading.Positions.Select(p => $"{p.PositionTitle}: {p.CardName} {(p.Reversed ? "reversed" : "upright")}"));
            return [new RuleDigestItem("tarot.base.spread", "Spread", text, 100)];
        }),
        new("tarot.stats.elements", "tarot", "Element stats", "Counts major arcana, suits, and reversed cards.", 100, true, ctx =>
        {
            var reading = (TarotReading)ctx.Chart;
            var names = reading.Positions.Select(p => p.CardName).ToList();
            var text = $"Major {names.Count(n => !n.Contains(" of ", StringComparison.Ordinal))}/{names.Count}; Wands {CountSuit(names, "Wands")}; Cups {CountSuit(names, "Cups")}; Swords {CountSuit(names, "Swords")}; Pentacles {CountSuit(names, "Pentacles")}; Reversed {reading.Positions.Count(p => p.Reversed)}";
            return [new RuleDigestItem("tarot.stats.elements", "Stats", text, 100)];
        }),
        new("tarot.element.meaning", "tarot", "Element tendency", "Maps the dominant minor-arcana suit to a reading theme.", 90, true, ctx =>
        {
            var reading = (TarotReading)ctx.Chart;
            var names = reading.Positions.Select(p => p.CardName).ToList();
            var counts = new[]
            {
                ("Wands", CountSuit(names, "Wands"), "action, will, creative force"),
                ("Cups", CountSuit(names, "Cups"), "emotion, relationship, intuition"),
                ("Swords", CountSuit(names, "Swords"), "thought, speech, conflict"),
                ("Pentacles", CountSuit(names, "Pentacles"), "resources, body, practical affairs")
            };
            var top = counts.OrderByDescending(x => x.Item2).First();
            return top.Item2 == 0
                ? []
                : [new RuleDigestItem("tarot.element.meaning", "Element tendency", $"{top.Item1} is most visible: emphasize {top.Item3}.", 90)];
        }),
        new("tarot.interpretation.waite", "tarot", "Waite meaning snippets", "Uses current card meaning fields as deterministic Layer1 text.", 80, true, ctx =>
        {
            var reading = (TarotReading)ctx.Chart;
            var text = string.Join("; ", reading.Positions.Select(p => $"{p.PositionTitleZh}: {p.Meaning}"));
            return [new RuleDigestItem("tarot.interpretation.waite", "Meanings", text, 80)];
        })
    ];

    private static int CountSuit(IReadOnlyList<string> names, string suit) =>
        names.Count(n => n.EndsWith($"of {suit}", StringComparison.Ordinal));
}
