namespace IChing.Lab.Core.Tarot;

public static class TarotDeck
{
    private static readonly Lazy<IReadOnlyList<TarotCard>> Deck = new(Build);

    public static IReadOnlyList<TarotCard> All => Deck.Value;

    public static IReadOnlyList<TarotCard> MajorOnly => MajorArcana;

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
                    $"{rank.En} of {suit.En}",
                    $"{suit.Zh}{rank.Zh}",
                    $"{suit.Zh}{rank.Zh} - {suit.Upright}",
                    $"{suit.Zh}{rank.Zh} - {suit.Reversed}",
                    false));
            }
        }
        return cards;
    }

    private static readonly (string En, string Zh, string Upright, string Reversed)[] Suits =
    [
        ("Wands", "权杖", "行动、热情、创造力", "冲动、停滞或能量分散"),
        ("Cups", "圣杯", "情感、关系、直觉", "情绪波动、逃避或失衡"),
        ("Swords", "宝剑", "思维、判断、沟通", "焦虑、僵持或冲突"),
        ("Pentacles", "星币", "现实、资源、身体与工作", "匮乏感、固执或现实压力")
    ];

    private static readonly (string En, string Zh)[] Ranks =
    [
        ("Ace", "王牌"), ("Two", "二"), ("Three", "三"), ("Four", "四"), ("Five", "五"),
        ("Six", "六"), ("Seven", "七"), ("Eight", "八"), ("Nine", "九"), ("Ten", "十"),
        ("Page", "侍从"), ("Knight", "骑士"), ("Queen", "皇后"), ("King", "国王")
    ];

    private static readonly TarotCard[] MajorArcana =
    [
        new(0, "The Fool", "愚者", "新开始、信任直觉、自由探索", "鲁莽、逃避责任、准备不足", true),
        new(1, "The Magician", "魔术师", "资源整合、行动力、显化", "操控、分心、能力未发挥", true),
        new(2, "The High Priestess", "女祭司", "直觉、潜意识、静观", "隐瞒、迟疑、忽视直觉", true),
        new(3, "The Empress", "皇后", "滋养、丰盛、创造", "过度依赖、停滞、消耗", true),
        new(4, "The Emperor", "皇帝", "秩序、边界、掌控", "僵化、独断、控制过度", true),
        new(5, "The Hierophant", "教皇", "传统、学习、指引", "教条、盲从、形式束缚", true),
        new(6, "The Lovers", "恋人", "选择、连结、价值一致", "失衡、犹豫、关系拉扯", true),
        new(7, "The Chariot", "战车", "意志、推进、胜利", "失控、好胜、方向冲突", true),
        new(8, "Strength", "力量", "勇气、耐心、温柔的控制", "软弱、压抑、信心不足", true),
        new(9, "The Hermit", "隐者", "内省、独处、寻找答案", "孤立、逃避、拒绝交流", true),
        new(10, "Wheel of Fortune", "命运之轮", "周期、转机、变化", "停滞、失控感、抗拒变化", true),
        new(11, "Justice", "正义", "公平、因果、清晰判断", "偏见、不公、逃避责任", true),
        new(12, "The Hanged Man", "倒吊人", "换位、暂停、放下执念", "僵持、无谓牺牲、拖延", true),
        new(13, "Death", "死神", "结束、转化、告别旧阶段", "抗拒改变、无法放手", true),
        new(14, "Temperance", "节制", "调和、节奏、整合", "极端、失衡、消耗", true),
        new(15, "The Devil", "恶魔", "执念、欲望、束缚", "觉醒、松绑、看见依附", true),
        new(16, "The Tower", "高塔", "突变、真相、结构崩塌", "害怕改变、延迟爆发", true),
        new(17, "The Star", "星星", "希望、疗愈、信心", "失望、信心不足、愿景模糊", true),
        new(18, "The Moon", "月亮", "迷雾、情绪、潜意识", "误解、焦虑、真相渐明", true),
        new(19, "The Sun", "太阳", "清晰、活力、喜悦", "短暂乐观、过度自信", true),
        new(20, "Judgement", "审判", "觉醒、召唤、复盘", "自我怀疑、拒绝回应", true),
        new(21, "The World", "世界", "完成、整合、阶段圆满", "未完成、停滞、临门一脚", true)
    ];
}
