using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Readings.Producers;
using IChing.Lab.Presentation;

namespace IChing.App.Services;

public static class HtmlReadingTemplate
{
    public static string BuildBazi(BaziChart chart, BaziRuleDigest digest, string? focus, string? interpretation)
    {
        var output = ReadingOutputParser.BuildExchangeOutput("bazi", interpretation, null, "remote", isFallback: false);
        var exchange = ReadingExchangeFactory.CreateInitial(
            "bazi",
            1,
            new ExchangeInput(null, focus, [], [digest.PillarSummary, digest.YongShenSummary], []),
            output);
        var vm = ReadingResultProducerRegistry.Produce(exchange, chart);
        return ReadingViewPresenter.ToDocument(vm, chart);
    }

    public static string BuildLiuyao(LiuyaoNajiaResult chart, LiuyaoRuleDigest digest, string? question, string? interpretation)
    {
        var output = ReadingOutputParser.BuildExchangeOutput("liuyao", interpretation, null, "remote", isFallback: false);
        var exchange = ReadingExchangeFactory.CreateInitial(
            "liuyao",
            1,
            new ExchangeInput(question, null, [], [digest.ShiYaoSummary, digest.YongShenSummary], []),
            output);
        var vm = ReadingResultProducerRegistry.Produce(exchange, chart);
        return ReadingViewPresenter.ToDocument(vm, chart);
    }

    public static string HexagramName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var trimmed = name.Trim();
        var display = HexagramNames.Display(trimmed);
        return display.Any(c => c is >= 'A' and <= 'z') ? "卦名待补" : display;
    }
}
