namespace IChing.Lab.Core.Tarot;

public static class TarotDeck
{
    private static readonly Lazy<IReadOnlyList<TarotCard>> Deck = new(Build);

    public static IReadOnlyList<TarotCard> All => Deck.Value;

    private static IReadOnlyList<TarotCard> Build()
    {
        var cards = new List<TarotCard>(78);
        cards.AddRange(MajorArcana);
        foreach (var suit in Suits)
        {
            foreach (var rank in Ranks)
            {
                cards.Add(new TarotCard(
                    cards.Count,
                    $"{rank} of {suit.Name}",
                    $"{rank} · {suit.Upright}",
                    $"{rank} · {suit.Reversed}"));
            }
        }
        return cards;
    }

    private static readonly (string Name, string Upright, string Reversed)[] Suits =
    [
        ("Wands", "行动与热情", "冲动或倦怠"),
        ("Cups", "情感与关系", "情绪波动或逃避"),
        ("Swords", "思维与决断", "焦虑或僵持"),
        ("Pentacles", "物质与稳定", "匮乏感或固执")
    ];

    private static readonly string[] Ranks =
    [
        "Ace", "Two", "Three", "Four", "Five",
        "Six", "Seven", "Eight", "Nine", "Ten",
        "Page", "Knight", "Queen", "King"
    ];

    private static readonly TarotCard[] MajorArcana =
    [
        new(0, "The Fool", "新开始、信任直觉", "鲁莽、逃避责任"),
        new(1, "The Magician", "资源整合、行动力", "操控、能量分散"),
        new(2, "The High Priestess", "直觉、潜意识", "隐瞒、迟疑"),
        new(3, "The Empress", "滋养、丰盛", "过度依赖、停滞"),
        new(4, "The Emperor", "秩序、掌控", "僵化、独断"),
        new(5, "The Hierophant", "传统、指引", "教条、盲从"),
        new(6, "The Lovers", "选择、联结", "失衡、犹豫"),
        new(7, "The Chariot", "意志、胜利", "失控、好斗"),
        new(8, "Strength", "勇气、耐心", "软弱、压抑"),
        new(9, "The Hermit", "内省、独处", "孤立、逃避"),
        new(10, "Wheel of Fortune", "周期、转机", "停滞、厄运感"),
        new(11, "Justice", "公平、因果", "偏见、不公"),
        new(12, "The Hanged Man", "换位、暂停", "僵持、无谓牺牲"),
        new(13, "Death", "结束、转化", "抗拒改变"),
        new(14, "Temperance", "调和、节奏", "极端、失衡"),
        new(15, "The Devil", "执念、欲望", "解脱、觉醒"),
        new(16, "The Tower", "突变、真相", "恐惧变化"),
        new(17, "The Star", "希望、疗愈", "失望、信心不足"),
        new(18, "The Moon", "迷雾、情绪", "误解、焦虑"),
        new(19, "The Sun", "清晰、活力", "短暂乐观"),
        new(20, "Judgement", "觉醒、召唤", "自我怀疑"),
        new(21, "The World", "完成、整合", "未完成、停滞")
    ];
}
