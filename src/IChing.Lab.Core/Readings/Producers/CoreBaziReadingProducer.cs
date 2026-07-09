using System.Text.Json;
using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Readings;

namespace IChing.Lab.Core.Readings.Producers;

public sealed class CoreBaziReadingProducer : IReadingResultProducer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string ProducerId => "core.bazi";

    public bool CanProduce(ReadingExchange exchange) =>
        string.Equals(exchange.Meta.Domain, "bazi", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(exchange.Meta.Mode, "entertainment", StringComparison.OrdinalIgnoreCase);

    public ReadingViewModel Produce(ReadingExchange exchange, object? chartRef)
    {
        var chart = chartRef as BaziChart;
        var (sections, summary) = SectionsFromOutput(exchange, "bazi");
        var focus = exchange.Input.Focus ?? chart?.DayMaster ?? "综合";
        var widgets = chart is null
            ? Array.Empty<ReadingWidgetVm>()
            :
            [
                new ReadingWidgetVm("pillarGrid", "四柱", JsonSerializer.Serialize(new
                {
                    dayMaster = chart.DayMaster,
                    wallClock = chart.WallClock,
                    lunar = chart.Lunar,
                    pillars = new[]
                    {
                        new { label = "年柱", ganZhi = chart.YearPillar.GanZhi },
                        new { label = "月柱", ganZhi = chart.MonthPillar.GanZhi },
                        new { label = "日柱", ganZhi = chart.DayPillar.GanZhi },
                        new { label = "时柱", ganZhi = chart.HourPillar.GanZhi }
                    }
                }, JsonOptions)),
                new ReadingWidgetVm("daYunTimeline", "大运", JsonSerializer.Serialize(new
                {
                    items = (chart.DaYun ?? Array.Empty<DaYunPeriod>()).Take(5).Select(x => $"{x.StartAge}-{x.EndAge}岁 {x.GanZhi}")
                }, JsonOptions))
            ];

        return new ReadingViewModel(
            ProducerId,
            "bazi",
            "八字命盘",
            focus,
            summary,
            sections,
            widgets,
            "light");
    }

    internal static (IReadOnlyList<ReadingSectionVm> Sections, string Summary) SectionsFromOutput(
        ReadingExchange exchange,
        string domain)
    {
        var structured = exchange.Output?.Structured;
        if (structured is not null)
        {
            var sections = structured.Sections
                .Select(s => new ReadingSectionVm(s.Key, s.Title, s.Body))
                .ToList();
            return (sections, structured.Summary);
        }

        var raw = exchange.Output?.RawText;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return ([], string.Empty);
        }

        var parsed = ReadingOutputParser.TryParseStructured(raw, domain);
        if (parsed is not null)
        {
            return (
                parsed.Sections.Select(s => new ReadingSectionVm(s.Key, s.Title, s.Body)).ToList(),
                parsed.Summary);
        }

        return ([new ReadingSectionVm("overview", "解读", raw)], raw);
    }
}
