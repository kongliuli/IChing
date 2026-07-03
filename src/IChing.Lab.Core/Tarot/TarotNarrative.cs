namespace IChing.Lab.Core.Tarot;

/// <summary>Layer-1 deterministic narrative from spread positions.</summary>
public static class TarotNarrative
{
    public static TarotNarrativeResult Build(TarotReading reading)
    {
        var sections = reading.Positions
            .Select(p => $"{p.PositionTitle}：{p.CardName}{(p.Reversed ? "（逆位）" : "（正位）")} — {p.Meaning}")
            .ToList();

        var headline = reading.Question is { Length: > 0 } q
            ? $"关于「{q}」的{reading.SpreadTitle}解读"
            : $"{reading.SpreadTitle}解读";

        var summary = reading.Positions.Count switch
        {
            >= 10 => BuildCelticSummary(reading),
            7 => BuildHorseshoeSummary(reading),
            _ => BuildSimpleSummary(reading)
        };

        return new TarotNarrativeResult(
            reading.SpreadId,
            headline,
            summary,
            sections,
            reading.Seed);
    }

    private static string BuildSimpleSummary(TarotReading reading)
    {
        var present = reading.Positions.LastOrDefault();
        return present is null
            ? "牌阵信息不足。"
            : $"当前核心牌为{present.CardName}，提示：{present.Meaning}";
    }

    private static string BuildCelticSummary(TarotReading reading)
    {
        var present = reading.Positions[0];
        var challenge = reading.Positions[1];
        var outcome = reading.Positions[^1];
        return $"处境由{present.CardName}主导，挑战为{challenge.CardName}，最终走向指向{outcome.CardName}。";
    }

    private static string BuildHorseshoeSummary(TarotReading reading)
    {
        var past = reading.Positions[0];
        var present = reading.Positions[1];
        var future = reading.Positions[2];
        var advice = reading.Positions[5];
        return $"过去{past.CardName}影响当下{present.CardName}，趋势{future.CardName}；建议关注{advice.CardName}。";
    }
}

public record TarotNarrativeResult(
    string SpreadId,
    string Headline,
    string Summary,
    IReadOnlyList<string> Sections,
    int? Seed);
