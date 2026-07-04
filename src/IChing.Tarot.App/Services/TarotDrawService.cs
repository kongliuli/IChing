using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Core.Engines;
using IChing.Lab.Core.Services;
using IChing.Lab.Core.Tarot;
using IChing.Lab.Engines.Tarot;
using Microsoft.Extensions.DependencyInjection;

namespace IChing.Tarot.App.Services;

public sealed class TarotDrawService
{
    private static readonly IReadOnlyList<string> DefaultChain = ["iching-tarot-built-in"];
    private readonly ChartEngineRouter _router;

    public TarotDrawService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IChartEngine, TarotChartEngine>();
        _router = new ChartEngineRouter(services.BuildServiceProvider().GetServices<IChartEngine>());
    }

    public TarotDrawResult Draw(string spreadId, string? question, int? seed = null)
    {
        var (reading, engineId) = TarotDrawPipeline.Draw(_router, DefaultChain, spreadId, question, seed);
        return new TarotDrawResult(reading, engineId);
    }

    public IReadOnlyList<TarotSpread> ListSpreads() => SpreadCatalog.List();
}

public sealed record TarotDrawResult(TarotReading Reading, string EngineId);
