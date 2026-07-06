using IChing.Lab.Abstractions.Models;
using IChing.Lab.Abstractions.Prompts;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Services;
using IChing.Lab.Inference;
using IChing.Lab.Inference.Prompts;

namespace IChing.Lab.Api.Services;

public sealed class LiuyaoDemoService
{
    private readonly ChartEngineRouter _router;
    private readonly IConfiguration _configuration;
    private readonly ChartInterpretationOrchestrator _orchestrator;
    private readonly IReadOnlyDictionary<string, IPromptBuilder> _promptBuilders;

    public LiuyaoDemoService(
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

    public (LiuyaoNajiaResult Chart, string EngineId) Cast(string method, DateTimeOffset at, int? seed)
    {
        var args = new Dictionary<string, object?>
        {
            ["method"] = method,
            ["at"] = at,
            ["seed"] = seed
        };
        var chain = ChartEngineRouter.ResolveEngineChain(_configuration, "liuyao", "iching-sixlines-2.0.3");
        var routed = _router.Calculate("liuyao", args, chain);
        return (ChartResultMapper.AsLiuyaoChart(routed.Result, method, at, seed), routed.EngineId);
    }

    public async Task<DemoReadResult> InterpretAsync(
        LiuyaoNajiaResult chart,
        string engineId,
        int tier,
        string? question,
        string? focus,
        int maxTokens = 512,
        CancellationToken ct = default)
    {
        var digest = ReadingSummaries.BuildLiuyaoRuleDigest(chart, question, focus);
        var preview = ReadingSummaries.BuildLiuyaoTier0Preview(chart, question, focus);

        if (tier == 0)
        {
            return new DemoReadResult(
                0, preview.OneLiner, preview.Disclaimer, preview.OneLiner,
                null, false, null,
                "rules-only (ReadingSummaries，无 LLM)", null);
        }

        const string templateId = "liuyao-tier1-default";
        var builder = _promptBuilders[templateId];
        var ctx = new PromptContext(
            Chart: chart,
            RuleDigest: digest,
            Question: question ?? "综合",
            Focus: focus,
            MaxTokens: maxTokens,
            Engine: _orchestrator.ResolveEngineMetadata("liuyao", engineId));
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
