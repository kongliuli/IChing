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
        var deck = (spread.MajorOnly ? TarotDeck.MajorOnly : TarotDeck.All).ToArray();
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
                pos.TitleZh,
                pos.Context,
                card.Name,
                card.NameZh,
                card.ImageUrl,
                reversed,
                reversed ? card.ReversedMeaning : card.UprightMeaning
            ));
        }

        return new TarotReading(
            spread.Id,
            spread.Title,
            spread.TitleZh,
            spread.Description,
            spread.MajorOnly,
            question,
            seed,
            positions);
    }
}

public record TarotCard(int Id, string Name, string NameZh, string UprightMeaning, string ReversedMeaning, bool IsMajor)
{
    public string ImageUrl => $"/tarot/rws/{Name.ToLowerInvariant().Replace(" ", "-")}.jpeg";
}

public record TarotSpread(
    string Id,
    string Title,
    string TitleZh,
    string Description,
    string Category,
    string Difficulty,
    bool MajorOnly,
    IReadOnlyList<TarotPosition> Positions)
{
    public int CardCount => Positions.Count;
    public string DeckMode => MajorOnly ? "仅大阿卡纳" : "全牌 78 张";
}

public record TarotPosition(string Key, string Title, string TitleZh, string Context);

public record TarotPositionReading(
    string PositionKey,
    string PositionTitle,
    string PositionTitleZh,
    string PositionContext,
    string CardName,
    string CardNameZh,
    string ImageUrl,
    bool Reversed,
    string Meaning
);

public record TarotReading(
    string SpreadId,
    string SpreadTitle,
    string SpreadTitleZh,
    string SpreadDescription,
    bool MajorOnly,
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
            "单张牌",
            "快速抓取当前问题的核心原型或今日提醒，适合日签、是非倾向和轻量自检。",
            "daily",
            "easiest",
            true,
            [
                new("answer", "Answer", "答案", "当前最需要看见的一点")
            ]),
        ["past-present-future"] = new(
            "past-present-future",
            "Past-Present-Future",
            "过去-现在-未来",
            "用三张牌看一个议题的时间线：过去影响、当前核心和自然发展趋势。",
            "general",
            "easiest",
            false,
            [
                new("past", "Past", "过去", "已经发生的影响"),
                new("present", "Present", "现在", "当前核心议题"),
                new("future", "Future", "未来", "趋势与可能走向")
            ]),
        ["situation-action-outcome"] = new(
            "situation-action-outcome",
            "Situation-Action-Outcome",
            "现状-行动-结果",
            "面向决策的实用牌阵：先看局面，再看可采取的行动，最后看可能结果。",
            "decision",
            "easiest",
            false,
            [
                new("situation", "Situation", "现状", "问题现状"),
                new("action", "Action", "行动", "建议行动"),
                new("outcome", "Outcome", "结果", "可能结果")
            ]),
        ["mind-body-spirit"] = new(
            "mind-body-spirit",
            "Mind-Body-Spirit",
            "身心灵",
            "聚焦内在状态和人生主题，适合只用大阿卡纳看思想、身体能量与精神方向。",
            "self",
            "easiest",
            true,
            [
                new("mind", "Mind", "心智", "想法与判断"),
                new("body", "Body", "身体", "现实状态与精力"),
                new("spirit", "Spirit", "灵性", "内在方向")
            ]),
        ["choice"] = new(
            "choice",
            "Choice",
            "二选一",
            "比较两条路径的倾向、隐藏变量和当下建议，适合具体选择题。",
            "decision",
            "intermediate",
            false,
            [
                new("option-a", "Option A", "选项 A", "选择 A 的核心倾向"),
                new("option-b", "Option B", "选项 B", "选择 B 的核心倾向"),
                new("hidden-factor", "Hidden Factor", "隐藏因素", "容易忽略的变量"),
                new("advice", "Advice", "建议", "当下可采取的做法")
            ]),
        ["relationship"] = new(
            "relationship",
            "Relationship",
            "关系牌阵",
            "观察双方状态、连接点、挑战和建议，适合亲密关系、合作或重要互动。",
            "relationship",
            "intermediate",
            false,
            [
                new("you", "You", "你", "你的状态"),
                new("other", "Other", "对方", "对方或外部对象的状态"),
                new("bridge", "Bridge", "连接", "双方连接点"),
                new("challenge", "Challenge", "挑战", "主要阻碍"),
                new("advice", "Advice", "建议", "关系中的建议")
            ]),
        ["horseshoe"] = new(
            "horseshoe",
            "Horseshoe",
            "马蹄牌阵",
            "七张牌梳理背景、阻碍、外部影响和走向，适合复杂但仍可行动的问题。",
            "decision",
            "intermediate",
            false,
            [
                new("past", "Past", "过去", "过去影响"),
                new("present", "Present", "现在", "当前状况"),
                new("hidden", "Hidden Influence", "隐藏影响", "隐藏影响"),
                new("obstacle", "Obstacle", "阻碍", "阻碍"),
                new("external", "External Influence", "外部影响", "外部环境"),
                new("advice", "Advice", "建议", "建议"),
                new("outcome", "Outcome", "结果", "可能结果")
            ]),
        ["week-ahead"] = new(
            "week-ahead",
            "Week Ahead",
            "未来一周",
            "七张牌对应未来七天的节奏和提醒，适合日程规划与每周观察。",
            "daily",
            "intermediate",
            false,
            [
                new("day-1", "Day 1", "第 1 天", "第一天重点"),
                new("day-2", "Day 2", "第 2 天", "第二天重点"),
                new("day-3", "Day 3", "第 3 天", "第三天重点"),
                new("day-4", "Day 4", "第 4 天", "第四天重点"),
                new("day-5", "Day 5", "第 5 天", "第五天重点"),
                new("day-6", "Day 6", "第 6 天", "第六天重点"),
                new("day-7", "Day 7", "第 7 天", "第七天重点")
            ]),
        ["celtic-cross"] = new(
            "celtic-cross",
            "Celtic Cross",
            "凯尔特十字",
            "十张牌展开现状、挑战、根源、趋势、外部影响和结果，适合长问题和复盘。",
            "general",
            "advanced",
            false,
            [
                new("present", "Present Situation", "当前处境", "当前处境"),
                new("challenge", "Challenge", "挑战", "核心挑战/阻碍"),
                new("distant-past", "Distant Past", "深层过去", "深层根源"),
                new("recent-past", "Recent Past", "近期过去", "近期影响"),
                new("best-outcome", "Best Outcome", "最佳可能", "最佳可能"),
                new("near-future", "Near Future", "近期未来", "近期趋势"),
                new("approach", "Your Approach", "你的态度", "你的态度与做法"),
                new("external", "External Influences", "外部影响", "外部环境与他人"),
                new("hopes-fears", "Hopes and Fears", "希望与担忧", "希望与担心"),
                new("outcome", "Final Outcome", "最终结果", "最终结果")
            ])
    };

    public static TarotSpread Get(string id) =>
        Spreads.TryGetValue(id, out var spread) ? spread : Spreads["past-present-future"];

    public static IReadOnlyList<TarotSpread> List() => Spreads.Values.ToList();
}
