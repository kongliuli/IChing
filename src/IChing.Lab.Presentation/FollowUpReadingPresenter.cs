using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Readings.Producers;

namespace IChing.Lab.Presentation;

public static class FollowUpReadingPresenter
{
    public static string ToDocument(
        string domain,
        int tier,
        ExchangeInput input,
        object? chartRef,
        string rawText)
    {
        var output = ReadingOutputParser.BuildExchangeOutput(domain, rawText, null, "remote", false);
        var exchange = ReadingExchangeFactory.CreateFollowUp(
            input,
            domain,
            tier,
            "local",
            null,
            string.Empty,
            [],
            null) with { Output = output };
        var vm = ReadingResultProducerRegistry.Produce(exchange, chartRef);
        return ReadingViewPresenter.ToDocument(vm, chartRef);
    }
}
