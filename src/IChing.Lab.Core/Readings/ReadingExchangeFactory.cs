using System.Text.Json;
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

    public static ReadingExchange CreateFollowUp(
        ExchangeInput immutableInput,
        string domain,
        int tier,
        string sessionId,
        string? parentExchangeId,
        string userQuestion,
        IReadOnlyList<DialogueTurn> history,
        ReadingStructuredOutput? initialStructured = null)
    {
        var meta = new ExchangeMeta(
            ReadingSchemas.ExchangeV1,
            Guid.NewGuid().ToString("N"),
            sessionId,
            parentExchangeId,
            domain,
            "followup",
            tier,
            "zh-CN",
            DateTimeOffset.UtcNow);

        var render = new ExchangeRenderSpec(
            ReadingSchemas.OutputV2,
            [],
            [],
            "core-followup-json",
            "remote-json");

        var dialogue = new ExchangeDialogue(history, userQuestion, MaxRounds: 3);
        return new ReadingExchange(meta, immutableInput, render, dialogue, Output: null);
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

public static class FollowUpExchangeBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static (string SystemPrompt, string UserContext) BuildRemotePrompt(
        ReadingExchange exchange,
        ReadingStructuredOutput? initialStructured,
        string? rawInitialText = null)
    {
        var history = exchange.Dialogue?.History ?? [];
        var question = exchange.Dialogue?.UserQuestion;
        var structured = initialStructured ?? ReadingOutputParser.TryParseStructured(rawInitialText, exchange.Meta.Domain);
        var context = ExchangeContextCompactor.BuildFollowUpContext(
            exchange.Input,
            structured,
            history,
            question);
        var (system, _) = FollowUpPromptBuilder.Build(
            exchange.Meta.Domain,
            exchange.Input,
            structured,
            rawInitialText,
            history);
        return (system, context);
    }

    public static string SerializeInput(ExchangeInput input) =>
        JsonSerializer.Serialize(input, JsonOptions);

    public static ExchangeInput? DeserializeInput(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ExchangeInput>(json, JsonOptions);
    }
}
