using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Core.Rules.Plugins;

public static class TarotRulePlugins
{
    public static readonly IReadOnlyList<RulePlugin> All =
    [
        new("tarot.base.spread", "tarot", "牌阵基础", "牌阵、牌位、牌名、正逆位。", 100, true, ctx =>
        {
            var reading = (TarotReading)ctx.Chart;
            var text = $"{reading.SpreadTitleZh}，{reading.Positions.Count} 张牌：" +
                       string.Join("；", reading.Positions.Select(p => $"{p.PositionTitleZh}：{p.CardNameZh}{(p.Reversed ? "逆位" : "正位")}"));
            return [new RuleDigestItem("tarot.base.spread", "牌阵", text, 100)];
        }),
        new("tarot.stats.elements", "tarot", "元素统计", "统计大阿卡纳、牌组花色和逆位。", 100, true, ctx =>
        {
            var reading = (TarotReading)ctx.Chart;
            var names = reading.Positions.Select(p => p.CardName).ToList();
            var text = $"大阿卡纳 {names.Count(n => !n.Contains(" of ", StringComparison.Ordinal))}/{names.Count}；权杖 {CountSuit(names, "Wands")}；圣杯 {CountSuit(names, "Cups")}；宝剑 {CountSuit(names, "Swords")}；星币 {CountSuit(names, "Pentacles")}；逆位 {reading.Positions.Count(p => p.Reversed)}";
            return [new RuleDigestItem("tarot.stats.elements", "统计", text, 100)];
        }),
        new("tarot.element.meaning", "tarot", "元素倾向", "将占优的小阿卡纳花色映射为解读主题。", 90, true, ctx =>
        {
            var reading = (TarotReading)ctx.Chart;
            var names = reading.Positions.Select(p => p.CardName).ToList();
            var counts = new[]
            {
                ("权杖", CountSuit(names, "Wands"), "行动、意志与创造力"),
                ("圣杯", CountSuit(names, "Cups"), "情绪、关系与直觉"),
                ("宝剑", CountSuit(names, "Swords"), "思维、表达与冲突"),
                ("星币", CountSuit(names, "Pentacles"), "资源、身体与现实事务")
            };
            var top = counts.OrderByDescending(x => x.Item2).First();
            return top.Item2 == 0
                ? []
                : [new RuleDigestItem("tarot.element.meaning", "元素倾向", $"{top.Item1}最明显：重点关注{top.Item3}。", 90)];
        }),
        new("tarot.interpretation.waite", "tarot", "韦特牌义片段", "使用当前牌义字段作为确定性的第一层文本。", 80, true, ctx =>
        {
            var reading = (TarotReading)ctx.Chart;
            var text = string.Join("; ", reading.Positions.Select(p => $"{p.PositionTitleZh}: {p.Meaning}"));
            return [new RuleDigestItem("tarot.interpretation.waite", "牌义", text, 80)];
        })
    ];

    private static int CountSuit(IReadOnlyList<string> names, string suit) =>
        names.Count(n => n.EndsWith($"of {suit}", StringComparison.Ordinal));
}
