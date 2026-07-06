using IChing.Lab.Core.Tarot;

namespace IChing.Tarot.App.Services;

public static class FollowUpPromptTemplates
{
    public static (string SystemPrompt, string Context) Tarot(TarotReading reading, string? question, string interpretation)
    {
        var cards = string.Join("\n", reading.Positions.Select(p =>
            $"- [{p.PositionTitleZh}] {p.CardNameZh} / {p.CardName}：{(p.Reversed ? "逆位" : "正位")}，{p.Meaning}"));

        return (
            "你是塔罗追问助手。只能基于已抽出的牌阵、牌位、正逆位和原始解读回答；不能新增牌、换牌或改牌位。回答简短、具体、可执行。",
            $"""
            当前牌阵：{reading.SpreadTitleZh}
            最初问题：{question ?? reading.Question ?? "综合解读"}
            当前牌阵内容：
            {cards}
            初始解读：
            {interpretation}
            """);
    }
}
