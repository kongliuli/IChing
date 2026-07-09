using IChing.Lab.Abstractions.Readings;

namespace IChing.Lab.Core.Readings.Producers;

public interface IReadingResultProducer
{
    string ProducerId { get; }

    bool CanProduce(ReadingExchange exchange);

    ReadingViewModel Produce(ReadingExchange exchange, object? chartRef);
}
