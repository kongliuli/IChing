using IChing.Lab.Abstractions.Models;
using IChing.Lab.Abstractions.Readings;

namespace IChing.Lab.Core.Readings;

/// <summary>
/// Lab Inference 统一入口：ReadingExchange → PromptContext（保留 structured RuleDigest / Engine 元数据）。
/// </summary>
public static class ExchangeInferenceRouter
{
    public static PromptContext BuildInitialContext(
        ReadingExchange exchange,
        object chart,
        object? structuredRuleDigest,
        EngineMetadata? engine,
        IReadOnlyList<string>? moduleFocuses,
        int maxTokens) =>
        ExchangePromptAdapter.FromExchange(exchange, chart, maxTokens) with
        {
            RuleDigest = structuredRuleDigest ?? exchange.Input.RuleDigest,
            Engine = engine,
            ModuleFocuses = moduleFocuses
        };
}
