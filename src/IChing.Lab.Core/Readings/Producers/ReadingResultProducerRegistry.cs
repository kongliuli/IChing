using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Readings.Templates;

namespace IChing.Lab.Core.Readings.Producers;

public static class ReadingResultProducerRegistry
{
    private static readonly IReadOnlyList<IReadingResultProducer> Producers =
    [
        new CoreBaziReadingProducer(),
        new CoreLiuyaoReadingProducer(),
        new CoreTarotReadingProducer(),
        new EntertainmentQuizProducer()
    ];

    public static IReadingResultProducer? Resolve(ReadingExchange exchange) =>
        Producers.FirstOrDefault(p => p.CanProduce(exchange));

    public static ReadingViewModel Produce(ReadingExchange exchange, object? chartRef)
    {
        var producer = Resolve(exchange);
        if (producer is null)
        {
            throw new InvalidOperationException($"no producer for domain={exchange.Meta.Domain} mode={exchange.Meta.Mode}");
        }

        return producer.Produce(exchange, chartRef);
    }

    public static string ResolveProducerId(string domain, string mode) =>
        mode.Equals("entertainment", StringComparison.OrdinalIgnoreCase)
            ? "entertainment.quiz"
            : ReadingTemplateRegistry.ResolveInitial(domain).ProducerId;
}
