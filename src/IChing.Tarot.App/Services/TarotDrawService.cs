using IChing.Lab.Core.Tarot;

namespace IChing.Tarot.App.Services;

public sealed class TarotDrawService
{
    public TarotDrawResult Draw(string spreadId, string? question, int? seed = null)
    {
        var reading = TarotEngine.Draw(spreadId, question, seed);
        return new TarotDrawResult(reading, "iching-tarot-built-in");
    }

    public IReadOnlyList<TarotSpread> ListSpreads() => SpreadCatalog.List();
}

public sealed record TarotDrawResult(TarotReading Reading, string EngineId);
