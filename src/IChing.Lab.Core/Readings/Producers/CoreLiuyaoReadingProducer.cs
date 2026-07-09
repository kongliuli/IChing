using System.Text.Json;
using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Liuyao;

namespace IChing.Lab.Core.Readings.Producers;

public sealed class CoreLiuyaoReadingProducer : IReadingResultProducer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string ProducerId => "core.liuyao";

    public bool CanProduce(ReadingExchange exchange) =>
        string.Equals(exchange.Meta.Domain, "liuyao", StringComparison.OrdinalIgnoreCase);

    public ReadingViewModel Produce(ReadingExchange exchange, object? chartRef)
    {
        var chart = chartRef as LiuyaoNajiaResult;
        var (sections, summary) = CoreBaziReadingProducer.SectionsFromOutput(exchange, "liuyao");
        var subject = exchange.Input.Question ?? chart?.OriginalHexagram ?? "六爻";
        var widgets = chart is null
            ? Array.Empty<ReadingWidgetVm>()
            :
            [
                new ReadingWidgetVm("hexagramLines", "六爻", JsonSerializer.Serialize(new
                {
                    original = chart.OriginalHexagram,
                    changed = chart.ChangedHexagram,
                    method = chart.Method,
                    lines = chart.Lines.OrderByDescending(x => x.Index).Select(l => new
                    {
                        l.Position,
                        l.YinYang,
                        l.IsChanging,
                        l.SixKin,
                        l.StemBranch,
                        l.SixSpirit,
                        l.Role
                    })
                }, JsonOptions)),
                new ReadingWidgetVm("shiYingBadge", "世应", JsonSerializer.Serialize(new
                {
                    shi = chart.Lines.FirstOrDefault(l => l.Role.Contains("世", StringComparison.Ordinal))?.Position,
                    ying = chart.Lines.FirstOrDefault(l => l.Role.Contains("应", StringComparison.Ordinal))?.Position
                }, JsonOptions))
            ];

        return new ReadingViewModel(
            ProducerId,
            "liuyao",
            "六爻卦例",
            subject,
            summary,
            sections,
            widgets,
            "light");
    }
}
