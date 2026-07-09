using IChing.Lab.Abstractions.Readings;

namespace IChing.Lab.Core.Readings;

public static class ReadingEnvelopeBuilder
{
    public static ReadingEnvelopeV2 Build(
        string domain,
        int tier,
        object chart,
        object? ruleDigest,
        Tier0Preview preview,
        ExchangeOutput? output,
        string? paipanEngine,
        string? sessionId = null,
        string? parentExchangeId = null,
        string? promptTemplateId = null)
    {
        var exchangeId = Guid.NewGuid().ToString("N");
        var mode = tier <= 0 ? "tier0" : "initial";

        var meta = new ExchangeMeta(
            ReadingSchemas.ExchangeV1,
            exchangeId,
            sessionId,
            parentExchangeId,
            domain,
            mode,
            tier,
            "zh-CN",
            DateTimeOffset.UtcNow);

        var input = new ExchangeInput(
            Question: null,
            Focus: null,
            ComputedFacts: [],
            RuleDigest: [],
            PluginContext: []);

        var render = new ExchangeRenderSpec(
            ReadingSchemas.OutputV2,
            [],
            [],
            promptTemplateId,
            tier <= 0 ? "none" : "qwen-chatml");

        var exchange = new ReadingExchange(meta, input, render, Dialogue: null, output);

        return new ReadingEnvelopeV2(
            ReadingSchemas.EnvelopeV2,
            sessionId,
            exchange,
            chart,
            new Tier0PreviewDto(preview.OneLiner, preview.Disclaimer));
    }

    public static ExchangeOutput NarrativeToOutput(
        string domain,
        object narrative,
        string defaultEngineId)
    {
        var text = GetString(narrative, "text");
        var textEn = GetString(narrative, "textEn");
        var isFallback = GetBool(narrative, "isFallback");
        var fallbackReason = GetString(narrative, "fallbackReason");
        var promptTemplate = GetString(narrative, "promptTemplate");
        var engineId = defaultEngineId;

        return ReadingOutputParser.BuildExchangeOutput(
            domain,
            text,
            textEn,
            engineId,
            isFallback,
            fallbackReason,
            promptTemplate);
    }

    private static string? GetString(object obj, string name)
    {
        var prop = obj.GetType().GetProperty(name);
        return prop?.GetValue(obj) as string;
    }

    private static bool GetBool(object obj, string name)
    {
        var prop = obj.GetType().GetProperty(name);
        if (prop?.GetValue(obj) is bool b)
        {
            return b;
        }

        return false;
    }
}
