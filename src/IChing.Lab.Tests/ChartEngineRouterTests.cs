using IChing.Lab.Abstractions.Models;
using IChing.Lab.Core.Engines;
using IChing.Lab.Core.Services;
using IChing.Lab.Core.Tarot;
using IChing.Lab.Engines.Tarot;

namespace IChing.Lab.Tests;

public class ChartEngineRouterTests
{
    [Fact]
    public void Constructor_SameEngineIdDifferentDomains_BothRegistered()
    {
        var router = new ChartEngineRouter([new BaziChartEngine(), new CalendarEngine()]);
        Assert.Equal(2, router.All.Count);
    }

    [Fact]
    public void AsTarotReading_DeserializesOrFallsBack()
    {
        var reading = TarotEngine.Draw("single-card", "test", 3);
        var mapped = ChartResultMapper.AsTarotReading(reading, "single-card", "test", 3);
        Assert.Single(mapped.Positions);
    }

    [Fact]
    public void TarotDrawPipeline_EnrichesViaRouter()
    {
        var router = new ChartEngineRouter([new TarotChartEngine()]);
        var (reading, engineId) = TarotDrawPipeline.Draw(
            router,
            ["iching-tarot-built-in"],
            "single-card",
            "loop",
            11);

        Assert.Equal("iching-tarot-built-in", engineId);
        Assert.Single(reading.Positions);
        Assert.Contains(reading.Positions, p => TarotDeckData.FindByNameIgnoreCase(p.CardName) is not null);
    }

    [Fact]
    public void DeckauraCoverage_PercentMatchesEnrichedDeck()
    {
        var reading = TarotEngine.Draw("past-present-future", "coverage", 3);
        var enriched = TarotReadingEnricher.EnrichWithDeckaura(reading);
        var coverage = TarotReadingEnricher.DeckauraCoveragePercent(enriched);
        Assert.Equal(100d, coverage);
    }

    [Fact]
    public void Calculate_SkipsBridgeError_FallsBackToBuiltin()
    {
        var router = new ChartEngineRouter([new TarotChartEngine(), new TarotDeckauraDataEngine()]);
        var chain = new[] { "tarot-roxyapi-remote", "iching-tarot-built-in" };
        var result = router.Calculate("tarot", new Dictionary<string, object?>
        {
            ["spreadId"] = "single-card",
            ["question"] = "test"
        }, chain);

        Assert.Equal("iching-tarot-built-in", result.EngineId);
        Assert.IsType<TarotReading>(result.Result);
    }

    [Fact]
    public void EnrichWithDeckaura_ReplacesMeaningWithDeckauraText()
    {
        var reading = TarotEngine.Draw("past-present-future", "test", 7);
        var enriched = TarotReadingEnricher.EnrichWithDeckaura(reading);

        foreach (var pair in reading.Positions.Zip(enriched.Positions))
        {
            var deckaura = TarotDeckData.FindByNameIgnoreCase(pair.First.CardName);
            if (deckaura is null)
            {
                continue;
            }

            var expected = pair.First.Reversed ? deckaura.Reversed : deckaura.Upright;
            Assert.Equal(expected, pair.Second.Meaning);
        }

        Assert.Contains(enriched.Positions, p => TarotDeckData.FindByNameIgnoreCase(p.CardName) is not null);
    }
}
