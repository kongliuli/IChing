using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Tarot;

namespace IChing.Tarot.App.Services;

public static class FollowUpPromptTemplates
{
    public static ExchangeInput TarotExchangeInput(TarotReading reading, string? question) =>
        new(
            Question: question ?? reading.Question,
            Focus: null,
            ComputedFacts:
            [
                $"spread: {reading.SpreadTitleZh}",
                ..reading.Positions.Select(p =>
                    $"[{p.PositionTitleZh}] {p.CardNameZh} {(p.Reversed ? "逆位" : "正位")}")
            ],
            RuleDigest: reading.Positions.Select(p => p.Meaning).ToArray(),
            PluginContext: []);
}
