using IChing.Lab.Abstractions.Models;
using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Readings.Templates;

namespace IChing.Lab.Core.Readings;

/// <summary>
/// 将 ReadingExchange 适配为 PromptContext / ReadingPromptPacket（OpenAI JSON 路径）。
/// </summary>
public static class ExchangePromptAdapter
{
    public static PromptContext FromExchange(ReadingExchange exchange, object? chartRef, int maxTokens = 512) =>
        new(
            Chart: chartRef ?? exchange.Input,
            RuleDigest: exchange.Input.RuleDigest,
            Question: exchange.Dialogue?.UserQuestion ?? exchange.Input.Question,
            Focus: exchange.Input.Focus,
            MaxTokens: maxTokens,
            Engine: null,
            ModuleFocuses: null);

    public static ReadingPromptPacket ToFollowUpPacket(
        ReadingExchange exchange,
        ReadingStructuredOutput? initialStructured,
        string? rawInitialText = null)
    {
        var structured = initialStructured ?? ReadingOutputParser.TryParseStructured(rawInitialText, exchange.Meta.Domain);
        var contextJson = ExchangeContextCompactor.BuildFollowUpContext(
            exchange.Input,
            structured,
            exchange.Dialogue?.History ?? [],
            exchange.Dialogue?.UserQuestion);

        var template = ReadingPromptTemplateManager.Get(exchange.Meta.Domain, "followup");
        return new ReadingPromptPacket(
            Schema: "reading-request.v1",
            OutputSchema: ReadingSchemas.OutputV2,
            Domain: exchange.Meta.Domain,
            Mode: "followup",
            Language: "zh-CN",
            Tier: exchange.Meta.Tier,
            Question: exchange.Input.Question,
            Focus: exchange.Input.Focus,
            ComputedFacts: exchange.Input.ComputedFacts,
            RuleDigest: exchange.Input.RuleDigest,
            SystemDirectives: template.SystemDirectives,
            PluginContext: exchange.Input.PluginContext
                .Select(p => new PluginPromptContext(p.PluginId, p.Facts, [], p.OutputSections, []))
                .ToList(),
            OutputSections: template.OutputSections,
            Prior: contextJson,
            UserQuestion: exchange.Dialogue?.UserQuestion);
    }

    public static string ResolveTemplateId(ReadingExchange exchange) =>
        exchange.Meta.Mode switch
        {
            "followup" => "core-followup-json",
            "translate" => "tarot-translate-to-zh",
            _ => ReadingTemplateRegistry.ResolveInitial(exchange.Meta.Domain, exchange.Meta.Tier).TemplateId
        };
}
