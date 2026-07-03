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
        var deck = TarotDeck.All.ToArray();
        for (var i = deck.Length - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }

        var positions = new List<TarotPositionReading>();
        for (var i = 0; i < spread.Positions.Count; i++)
        {
            var card = deck[i];
            var reversed = rng.Next(2) == 0;
            var pos = spread.Positions[i];
            positions.Add(new TarotPositionReading(
                pos.Key,
                pos.Title,
                pos.Context,
                card.Name,
                reversed,
                reversed ? card.ReversedMeaning : card.UprightMeaning
            ));
        }

        return new TarotReading(spread.Id, spread.Title, question, seed, positions);
    }
}

public record TarotCard(int Id, string Name, string UprightMeaning, string ReversedMeaning);

public record TarotSpread(
    string Id,
    string Title,
    string Description,
    string Category,
    string Difficulty,
    IReadOnlyList<TarotPosition> Positions)
{
    public int CardCount => Positions.Count;
}

public record TarotPosition(string Key, string Title, string Context);

public record TarotPositionReading(
    string PositionKey,
    string PositionTitle,
    string PositionContext,
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
        ["single-card"] = new(
            "single-card",
            "Single Card",
            "A fast daily or yes/no-oriented snapshot.",
            "daily",
            "easiest",
            [
                new("answer", "Answer", "当前最需要看见的一点")
            ]),
        ["past-present-future"] = new(
            "past-present-future",
            "Past-Present-Future",
            "A simple timeline for seeing what is fading, active, and emerging.",
            "general",
            "easiest",
            [
                new("past", "Past", "已发生的影响"),
                new("present", "Present", "当前核心议题"),
                new("future", "Future", "趋势与可能走向")
            ]),
        ["situation-action-outcome"] = new(
            "situation-action-outcome",
            "Situation-Action-Outcome",
            "A practical spread for decisions and next moves.",
            "decision",
            "easiest",
            [
                new("situation", "Situation", "问题现状"),
                new("action", "Action", "建议行动"),
                new("outcome", "Outcome", "可能结果")
            ]),
        ["mind-body-spirit"] = new(
            "mind-body-spirit",
            "Mind-Body-Spirit",
            "A compact self-check across thought, energy, and inner direction.",
            "self",
            "easiest",
            [
                new("mind", "Mind", "想法与判断"),
                new("body", "Body", "现实状态与精力"),
                new("spirit", "Spirit", "内在方向")
            ]),
        ["choice"] = new(
            "choice",
            "Choice",
            "Compares two paths without pretending the decision is already made.",
            "decision",
            "intermediate",
            [
                new("option-a", "Option A", "选择 A 的核心倾向"),
                new("option-b", "Option B", "选择 B 的核心倾向"),
                new("hidden-factor", "Hidden Factor", "容易忽略的变量"),
                new("advice", "Advice", "当下可采取的做法")
            ]),
        ["relationship"] = new(
            "relationship",
            "Relationship",
            "A basic relationship spread for two sides and the shared dynamic.",
            "relationship",
            "intermediate",
            [
                new("you", "You", "你的状态"),
                new("other", "Other", "对方或外部对象的状态"),
                new("bridge", "Bridge", "双方连接点"),
                new("challenge", "Challenge", "主要阻碍"),
                new("advice", "Advice", "关系中的建议")
            ]),
        ["horseshoe"] = new(
            "horseshoe",
            "Horseshoe",
            "A seven-card spread for context, obstacles, and likely direction.",
            "decision",
            "intermediate",
            [
                new("past", "Past", "过去影响"),
                new("present", "Present", "当前状况"),
                new("hidden", "Hidden Influence", "隐藏影响"),
                new("obstacle", "Obstacle", "阻碍"),
                new("external", "External Influence", "外部环境"),
                new("advice", "Advice", "建议"),
                new("outcome", "Outcome", "可能结果")
            ]),
        ["week-ahead"] = new(
            "week-ahead",
            "Week Ahead",
            "A seven-card look at the rhythm of the coming week.",
            "daily",
            "intermediate",
            [
                new("day-1", "Day 1", "第一天重点"),
                new("day-2", "Day 2", "第二天重点"),
                new("day-3", "Day 3", "第三天重点"),
                new("day-4", "Day 4", "第四天重点"),
                new("day-5", "Day 5", "第五天重点"),
                new("day-6", "Day 6", "第六天重点"),
                new("day-7", "Day 7", "第七天重点")
            ]),
        ["celtic-cross"] = new(
            "celtic-cross",
            "Celtic Cross",
            "A ten-card spread for layered situations and longer-form readings.",
            "general",
            "advanced",
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

    public static IReadOnlyList<TarotSpread> List() => Spreads.Values.ToList();
}
