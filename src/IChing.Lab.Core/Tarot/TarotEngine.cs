namespace IChing.Lab.Core.Tarot;

/// <summary>
/// Layer-1 deterministic tarot: spread schema + seeded shuffle + upright/reversed.
/// </summary>
public static class TarotEngine
{
    public static TarotReading Draw(string spreadId, string? question, int? seed = null)
    {
        var spread = SpreadCatalog.Get(spreadId);
        var rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        var deck = TarotDeck.All.OrderBy(_ => rng.Next()).ToList();

        var positions = new List<TarotPositionReading>();
        for (var i = 0; i < spread.Positions.Count; i++)
        {
            var card = deck[i];
            var reversed = rng.Next(2) == 0;
            var pos = spread.Positions[i];
            positions.Add(new TarotPositionReading(
                pos.Key,
                pos.Title,
                card.Name,
                reversed,
                reversed ? card.ReversedMeaning : card.UprightMeaning
            ));
        }

        return new TarotReading(spread.Id, spread.Title, question, seed, positions);
    }
}

public record TarotCard(int Id, string Name, string UprightMeaning, string ReversedMeaning);

public record TarotSpread(string Id, string Title, IReadOnlyList<TarotPosition> Positions);

public record TarotPosition(string Key, string Title, string Context);

public record TarotPositionReading(
    string PositionKey,
    string PositionTitle,
    string CardName,
    bool Reversed,
    string Meaning
);

public record TarotReading(
    string SpreadId,
    string SpreadTitle,
    string? Question,
    int? Seed,
    IReadOnlyList<TarotPositionReading> Positions
);

public static class SpreadCatalog
{
    private static readonly Dictionary<string, TarotSpread> Spreads = new()
    {
        ["past-present-future"] = new(
            "past-present-future",
            "Past-Present-Future",
            [
                new("past", "Past", "已发生的影响"),
                new("present", "Present", "当前核心议题"),
                new("future", "Future", "趋势与可能走向")
            ]),
        ["situation-action-outcome"] = new(
            "situation-action-outcome",
            "Situation-Action-Outcome",
            [
                new("situation", "Situation", "问题现状"),
                new("action", "Action", "建议行动"),
                new("outcome", "Outcome", "可能结果")
            ]),
        ["celtic-cross"] = new(
            "celtic-cross",
            "Celtic Cross",
            [
                new("present", "Present Situation", "当前处境"),
                new("challenge", "Challenge", "核心挑战/阻碍"),
                new("distant-past", "Distant Past", "深层根源"),
                new("recent-past", "Recent Past", "近期影响"),
                new("best-outcome", "Best Outcome", "最佳可能"),
                new("near-future", "Near Future", "近期趋势"),
                new("approach", "Your Approach", "你的态度与做法"),
                new("external", "External Influences", "外部环境与他人"),
                new("hopes-fears", "Hopes and Fears", "希望与担忧"),
                new("outcome", "Final Outcome", "最终结果")
            ])
    };

    public static TarotSpread Get(string id) =>
        Spreads.TryGetValue(id, out var spread) ? spread : Spreads["past-present-future"];

    public static IReadOnlyList<string> List() => Spreads.Keys.ToList();
}
