using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Readings.Producers;
using IChing.Lab.Presentation;
using IChing.Tarot.App.Models;

namespace IChing.Tarot.App.Services;

public static class QuizReadingProducerBridge
{
    public static ReadingViewModel ToViewModel(PersonalityQuizResult result, string scoring)
    {
        var input = new QuizProducerInput(
            scoring,
            result.Code,
            result.Title,
            result.Summary,
            result.Detail,
            result.DimensionScores,
            result.DimensionBars.Select(b => new QuizDimensionBar(b.Title, b.LeftLabel, b.RightLabel, b.LeftPercent)).ToList(),
            result.Sections.Select(s => new QuizProducerSection(Slug(s.Title), s.Title, s.Content)).ToList());

        var exchange = ReadingExchangeFactory.CreateEntertainment(input);
        return ReadingResultProducerRegistry.Produce(exchange, input);
    }

    public static string ToHtml(PersonalityQuizResult result, string scoring) =>
        ReadingViewPresenter.ToDocument(ToViewModel(result, scoring));

    private static string Slug(string title)
    {
        var slug = new string(title.Where(ch => char.IsLetterOrDigit(ch) || ch is >= '\u4e00' and <= '\u9fff').ToArray());
        return string.IsNullOrWhiteSpace(slug) ? "section" : slug[..Math.Min(slug.Length, 24)];
    }
}
