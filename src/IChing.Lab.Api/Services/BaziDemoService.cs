using IChing.Lab.Abstractions.Models;
using IChing.Lab.Abstractions.Prompts;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Services;
using IChing.Lab.Inference;

namespace IChing.Lab.Api.Services;

public sealed class BaziDemoService
{
    private readonly ChartEngineRouter _router;
    private readonly IConfiguration _configuration;
    private readonly ChartInterpretationOrchestrator _orchestrator;
    private readonly IReadOnlyDictionary<string, IPromptBuilder> _promptBuilders;

    public BaziDemoService(
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

    public (BaziChart Chart, string EngineId) Calculate(BaziInput input)
    {
        var chain = ChartEngineRouter.ResolveEngineChain(_configuration, "bazi", "lunar-csharp-1.6.8");
        var routed = _router.Calculate("bazi", ChartDemoHelper.ToArgs(input), chain);
        return (ChartResultMapper.AsBaziChart(routed.Result, input), routed.EngineId);
    }

    public async Task<DemoReadResult> InterpretAsync(
        BaziChart chart,
        string engineId,
        int tier,
        string? focus,
        int maxTokens = 512,
        CancellationToken ct = default)
    {
        var preview = ReadingSummaries.BuildBaziTier0Preview(chart, focus);
        if (tier == 0)
        {
            return new DemoReadResult(
                0, preview.OneLiner, preview.Disclaimer, preview.OneLiner,
                null, false, null,
                "rules-only (ReadingSummaries，无 LLM)", null);
        }

        const string templateId = "bazi-tier1-default";
        var builder = _promptBuilders[templateId];
        var ctx = new PromptContext(
            Chart: chart,
            RuleDigest: null,
            Question: null,
            Focus: focus,
            MaxTokens: maxTokens,
            Engine: _orchestrator.ResolveEngineMetadata("bazi", engineId));
        var prompt = builder.Build(ctx).PromptText;
        var gen = await _orchestrator.GenerateWithFallbackAsync(
            prompt, new GenerateOptions(MaxTokens: maxTokens), ct);

        return new DemoReadResult(
            tier, preview.OneLiner, preview.Disclaimer,
            string.IsNullOrWhiteSpace(gen.Text) ? preview.OneLiner : gen.Text,
            null, gen.IsFallback, gen.FallbackReason,
            templateId, prompt);
    }
}
