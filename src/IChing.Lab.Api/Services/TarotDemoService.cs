using IChing.Lab.Abstractions.Models;
using IChing.Lab.Abstractions.Prompts;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Services;
using IChing.Lab.Core.Tarot;
using IChing.Lab.Engines.Tarot;
using IChing.Lab.Inference;
using IChing.Lab.Inference.Prompts;

namespace IChing.Lab.Api.Services;

/// <summary>Blazor 塔罗演示：抽牌与解读均在进程内完成，基于当前牌阵（不重复抽牌）。</summary>
public sealed class TarotDemoService
{
    private readonly ChartEngineRouter _router;
    private readonly IConfiguration _configuration;
    private readonly ChartInterpretationOrchestrator _orchestrator;
    private readonly IReadOnlyDictionary<string, IPromptBuilder> _promptBuilders;

    public TarotDemoService(
        ChartEngineRouter router,
        IConfiguration configuration,
        ChartInterpretationOrchestrator orchestrator,
        IEnumerable<IPromptBuilder> promptBuilders)
    {
        _router = router;
        _configuration = configuration;
        _orchestrator = orchestrator;
        _promptBuilders = promptBuilders.ToDictionary(b => b.TemplateId);
    }

    public IReadOnlyList<TarotSpread> ListSpreads() => SpreadCatalog.List();

    public (TarotReading Reading, string EngineId) Draw(string? spreadId, string? question, int? seed)
    {
        var chain = ChartEngineRouter.ResolveEngineChain(_configuration, "tarot", "iching-tarot-built-in");
        return TarotDrawPipeline.Draw(_router, chain, spreadId, question, seed);
    }

    /// <summary>对已有牌阵解读：Tier 0 规则摘要；Tier 1+ 走 Scriban 模板 + 降级链。</summary>
    public async Task<DemoReadResult> InterpretAsync(
        TarotReading reading,
        string engineId,
        int tier,
        string? question,
        int maxTokens = 512,
        CancellationToken ct = default)
    {
        if (tier is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(tier), "tier must be 0, 1, or 2");
        }

        var preview = ReadingSummaries.BuildTarotTier0Preview(reading, question);

        if (tier == 0)
        {
            return new DemoReadResult(
                Tier: 0,
                OneLiner: preview.OneLiner,
                Disclaimer: preview.Disclaimer,
                Text: preview.OneLiner,
                TextEn: null,
                IsFallback: false,
                FallbackReason: null,
                PromptTemplate: "rules-only (TarotNarrative + ReadingSummaries，无 LLM)",
                PromptPreview: null);
        }

        var digest = TarotReadingEnricher.BuildEnrichedRuleDigest(reading);
        var positions = reading.Positions
            .Select(p => new TarotPositionPrompt(p.PositionTitleZh, p.PositionContext, p.CardNameZh, p.Reversed, p.Meaning))
            .ToList();
        var (templateId, useTranslatePass, wordLimit, tokenBudget) =
            ResolveTarotPrompt(engineId, tier, reading.SpreadId);
        var builder = ResolvePromptBuilder(templateId);
        var tarotCtx = new PromptContext(
            Chart: new TarotPromptInput(reading.SpreadTitleZh, positions, wordLimit),
            RuleDigest: digest,
            Question: question ?? "General reading",
            Focus: null,
            MaxTokens: maxTokens > 0 ? maxTokens : tokenBudget,
            Engine: _orchestrator.ResolveEngineMetadata("tarot", engineId));
        var prompt = builder.Build(tarotCtx).PromptText;
        var budget = maxTokens > 0 ? maxTokens : tokenBudget;

        if (useTranslatePass)
        {
            var result = _orchestrator.InterpretTarotEnglishThenChinese(prompt, budget, budget);
            return new DemoReadResult(
                Tier: tier,
                OneLiner: preview.OneLiner,
                Disclaimer: preview.Disclaimer,
                Text: string.IsNullOrWhiteSpace(result.TextZh) ? preview.OneLiner : result.TextZh,
                TextEn: result.TextEn,
                IsFallback: result.IsFallback,
                FallbackReason: result.FallbackReason,
                PromptTemplate: $"{templateId} + tarot-translate-to-zh",
                PromptPreview: prompt);
        }

        var gen = await _orchestrator.GenerateWithFallbackAsync(
            prompt,
            new GenerateOptions(MaxTokens: budget),
            ct);
        return new DemoReadResult(
            Tier: tier,
            OneLiner: preview.OneLiner,
            Disclaimer: preview.Disclaimer,
            Text: string.IsNullOrWhiteSpace(gen.Text) ? preview.OneLiner : gen.Text,
            TextEn: null,
            IsFallback: gen.IsFallback,
            FallbackReason: gen.FallbackReason,
            PromptTemplate: templateId,
            PromptPreview: prompt);
    }

    private IPromptBuilder ResolvePromptBuilder(string templateId) =>
        _promptBuilders.TryGetValue(templateId, out var builder)
            ? builder
            : throw new InvalidOperationException($"prompt builder not registered: {templateId}");

    private static (string TemplateId, bool UseTranslatePass, int WordLimit, int MaxTokens) ResolveTarotPrompt(
        string engineId,
        int tier,
        string spreadId)
    {
        if (tier == 2 && string.Equals(spreadId, "celtic-cross", StringComparison.OrdinalIgnoreCase))
        {
            return ("tarot-tier2-celtic-cross", false, 900, 1200);
        }

        if (engineId.StartsWith("tarot-deckaura", StringComparison.OrdinalIgnoreCase)
            || string.Equals(engineId, "iching-tarot-built-in", StringComparison.OrdinalIgnoreCase))
        {
            var wordLimit = tier == 2 ? 700 : 400;
            return ("tarot-tier1-deckaura-default", false, wordLimit, tier == 2 ? 900 : 512);
        }

        var limit = tier == 2 ? 800 : (spreadId == "celtic-cross" ? 500 : 280);
        return ("tarot-tier1-en", true, limit, tier == 2 ? 1024 : 512);
    }
}
