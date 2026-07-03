namespace IChing.Lab.Core.Tarot;

/// <summary>
/// Layer-1 deterministic tarot: spread schema + seeded shuffle + upright/reversed.
/// Layer-2 LLM synthesis is out of scope here.
/// </summary>
public static class TarotEngine
{
    private static readonly TarotCard[] MajorArcana =
    [
        new(0, "The Fool", "新开始、冒险", "鲁莽、逃避"),
        new(1, "The Magician", "行动力、资源齐备", "操控、欺骗"),
        new(2, "The High Priestess", "直觉、潜意识", "隐瞒、迟疑"),
        new(3, "The Empress", "丰饶、滋养", "依赖、停滞"),
        new(4, "The Emperor", "结构、权威", "僵化、控制"),
        new(5, "The Hierophant", "传统、指引", "教条、束缚"),
        new(6, "The Lovers", "选择、联结", "失衡、犹豫"),
        new(7, "The Chariot", "意志、推进", "失控、冲动"),
        new(8, "Strength", "耐心、勇气", "软弱、压抑"),
        new(9, "The Hermit", "内省、独处", "孤立、逃避"),
        new(10, "Wheel of Fortune", "周期、转机", "停滞、厄运感"),
        new(11, "Justice", "公平、因果", "偏见、延迟"),
        new(12, "The Hanged Man", "换位、暂停", "僵持、牺牲感"),
        new(13, "Death", "结束、转化", "抗拒改变"),
        new(14, "Temperance", "调和、节奏", "极端、失衡"),
        new(15, "The Devil", "执念、欲望", "解脱、觉醒"),
        new(16, "The Tower", "突变、拆解", "恐惧变化"),
        new(17, "The Star", "希望、疗愈", "失望、信心不足"),
        new(18, "The Moon", "迷雾、情绪", "误解、焦虑"),
        new(19, "The Sun", "清晰、活力", "短暂乐观"),
        new(20, "Judgement", "觉醒、召唤", "自我怀疑"),
        new(21, "The World", "完成、整合", "停滞、未完成")
    ];

    public static TarotReading Draw(string spreadId, string? question, int? seed = null)
    {
        var spread = SpreadCatalog.Get(spreadId);
        var rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        var deck = MajorArcana.OrderBy(_ => rng.Next()).ToList();

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
            ])
    };

    public static TarotSpread Get(string id) =>
        Spreads.TryGetValue(id, out var spread) ? spread : Spreads["past-present-future"];
}
