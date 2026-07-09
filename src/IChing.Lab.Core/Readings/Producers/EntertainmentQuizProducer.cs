using System.Text.Json;
using IChing.Lab.Abstractions.Readings;

namespace IChing.Lab.Core.Readings.Producers;

public sealed class EntertainmentQuizProducer : IReadingResultProducer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string ProducerId => "entertainment.quiz";

    public bool CanProduce(ReadingExchange exchange) =>
        string.Equals(exchange.Meta.Mode, "entertainment", StringComparison.OrdinalIgnoreCase);

    public ReadingViewModel Produce(ReadingExchange exchange, object? chartRef)
    {
        var input = chartRef as QuizProducerInput;
        var sections = input?.Sections?.Select(s => new ReadingSectionVm(s.Key, s.Title, s.Body)).ToList()
                       ?? CoreBaziReadingProducer.SectionsFromOutput(exchange, "entertainment").Sections.ToList();
        var summary = input?.Summary ?? exchange.Output?.Structured?.Summary ?? string.Empty;
        var widgets = new List<ReadingWidgetVm>();
        if (input?.DimensionBars is { Count: > 0 })
        {
            widgets.Add(new ReadingWidgetVm(
                "dimensionBars",
                "维度",
                JsonSerializer.Serialize(input.DimensionBars, JsonOptions)));
        }

        if (!string.IsNullOrWhiteSpace(input?.Code))
        {
            widgets.Add(new ReadingWidgetVm(
                "typeCard",
                input.Title,
                JsonSerializer.Serialize(new { code = input.Code, title = input.Title, summary }, JsonOptions)));
        }

        return new ReadingViewModel(
            ProducerId,
            "entertainment",
            input?.Title ?? "人格测评",
            input?.Scoring ?? "quiz",
            summary,
            sections,
            widgets,
            "tarot");
    }
}

public sealed record QuizProducerSection(string Key, string Title, string Body);

public sealed record QuizDimensionBar(string Title, string LeftLabel, string RightLabel, int LeftPercent);

public sealed record QuizProducerInput(
    string Scoring,
    string Code,
    string Title,
    string Summary,
    string Detail,
    IReadOnlyDictionary<string, int> Totals,
    IReadOnlyList<QuizDimensionBar>? DimensionBars = null,
    IReadOnlyList<QuizProducerSection>? Sections = null);
