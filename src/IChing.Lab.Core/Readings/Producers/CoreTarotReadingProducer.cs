using System.Text.Json;
using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Core.Readings.Producers;

public sealed class CoreTarotReadingProducer : IReadingResultProducer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string ProducerId => "core.tarot";

    public bool CanProduce(ReadingExchange exchange) =>
        string.Equals(exchange.Meta.Domain, "tarot", StringComparison.OrdinalIgnoreCase);

    public ReadingViewModel Produce(ReadingExchange exchange, object? chartRef)
    {
        var reading = chartRef as TarotReading;
        var (sections, summary) = CoreBaziReadingProducer.SectionsFromOutput(exchange, "tarot");
        var subject = exchange.Input.Question ?? reading?.Question ?? "塔罗";
        var widgets = reading is null
            ? Array.Empty<ReadingWidgetVm>()
            :
            [
                new ReadingWidgetVm("spreadTable", "牌阵", JsonSerializer.Serialize(new
                {
                    spread = reading.SpreadTitleZh,
                    positions = reading.Positions.Select(p => new
                    {
                        p.PositionTitleZh,
                        p.CardNameZh,
                        reversed = p.Reversed,
                        p.Meaning
                    })
                }, JsonOptions))
            ];

        return new ReadingViewModel(
            ProducerId,
            "tarot",
            reading?.SpreadTitleZh ?? "塔罗解读",
            subject,
            summary,
            sections,
            widgets,
            "tarot");
    }
}
