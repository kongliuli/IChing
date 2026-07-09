using IChing.Lab.Core.Services;
using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Engines.Tarot;

/// <summary>塔罗抽牌 + Deckaura enrich 的统一管线（Lab API / MAUI / sidecar 共用）。</summary>
public static class TarotDrawPipeline
{
    public static (TarotReading Reading, string EngineId) Draw(
        ChartEngineRouter router,
        IReadOnlyList<string> engineChain,
        string? spreadId,
        string? question,
        int? seed)
    {
        var resolvedSpread = spreadId ?? "past-present-future";
        var args = new Dictionary<string, object?>
        {
            ["spreadId"] = resolvedSpread,
            ["question"] = question,
            ["seed"] = seed
        };

        var routed = router.Calculate("tarot", args, engineChain);
        var reading = ChartResultMapper.AsTarotReading(routed.Result, resolvedSpread, question, seed);
        return (TarotReadingEnricher.EnrichWithDeckaura(reading), routed.EngineId);
    }
}
