using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Readings.Producers;

namespace IChing.Lab.Core.Readings;

public static class ReadingExchangeFactory
{
    public static ReadingExchange CreateInitial(
        string domain,
        int tier,
        ExchangeInput input,
        ExchangeOutput? output,
        string? promptTemplateId = null,
        string? sessionId = null)
    {
        var mode = tier <= 0 ? "tier0" : "initial";
        var meta = new ExchangeMeta(
            ReadingSchemas.ExchangeV1,
            Guid.NewGuid().ToString("N"),
            sessionId,
            null,
            domain,
            mode,
            tier,
            "zh-CN",
            DateTimeOffset.UtcNow);

        var render = new ExchangeRenderSpec(
            ReadingSchemas.OutputV2,
            [],
            [],
            promptTemplateId,
            tier <= 0 ? "none" : "remote-json");

        return new ReadingExchange(meta, input, render, Dialogue: null, output);
    }

    public static ReadingExchange CreateEntertainment(QuizProducerInput quiz)
    {
        var meta = new ExchangeMeta(
            ReadingSchemas.ExchangeV1,
            Guid.NewGuid().ToString("N"),
            null,
            null,
            "entertainment",
            "entertainment",
            0,
            "zh-CN",
            DateTimeOffset.UtcNow);

        var input = new ExchangeInput(
            null,
            null,
            [$"scoring:{quiz.Scoring}", $"code:{quiz.Code}"],
            [],
            []);

        var render = new ExchangeRenderSpec(ReadingSchemas.OutputV2, [], [], null, "none");
        var structuredSections = quiz.Sections?
            .Select(s => new ReadingStructuredSection(s.Key, s.Title, s.Body))
            .ToList() ?? [];
        var output = new ExchangeOutput(
            new ReadingStructuredOutput(ReadingSchemas.OutputV2, quiz.Summary, structuredSections, []),
            quiz.Detail,
            null,
            "rules",
            false);

        return new ReadingExchange(meta, input, render, Dialogue: null, output);
    }
}
