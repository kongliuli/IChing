using IChing.Lab.Abstractions.Readings;

namespace IChing.Lab.Core.Readings.Producers;

/// <summary>
/// 商业版诱导内容：每日一签 / 追问引导（无 LLM）。
/// </summary>
public sealed class EngagementReadingProducer : IReadingResultProducer
{
    public string ProducerId => "engagement.daily";

    public bool CanProduce(ReadingExchange exchange) =>
        string.Equals(exchange.Meta.Domain, "engagement", StringComparison.OrdinalIgnoreCase);

    public ReadingViewModel Produce(ReadingExchange exchange, object? chartRef)
    {
        var tip = exchange.Input.Question ?? "今日宜静观其变，少做重大决定。";
        return new ReadingViewModel(
            ProducerId,
            "engagement",
            "今日提示",
            tip,
            tip,
            [
                new ReadingSectionVm("nudge", "引导", "若仍有困惑，可就一个具体问题发起追问（商业版）。")
            ],
            Array.Empty<ReadingWidgetVm>(),
            "engagement");
    }
}
