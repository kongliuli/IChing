using IChing.Lab.Abstractions.Models;
using IChing.Lab.Abstractions.Prompts;
using IChing.Lab.Api.Contracts;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Rules;
using IChing.Lab.Core.Tarot;
using IChing.Lab.Engines.Tarot;
using IChing.Lab.Inference;
using IChing.Lab.Inference.Prompts;
using Microsoft.AspNetCore.Mvc;

namespace IChing.Lab.Api.Services;

public sealed class LabReadService
{
    private readonly ChartInterpretationOrchestrator _interpretation;
    private readonly LabChartQueryService _charts;
    private readonly RuleEngine _ruleEngine;
    private readonly AccountsCreditsGateway _accounts;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IReadOnlyDictionary<string, IPromptBuilder> _promptBuilders;

    public LabReadService(
        ChartInterpretationOrchestrator interpretation,
        LabChartQueryService charts,
        RuleEngine ruleEngine,
        AccountsCreditsGateway accounts,
        IHttpContextAccessor httpContextAccessor,
        IEnumerable<IPromptBuilder> promptBuilders)
    {
        _interpretation = interpretation;
        _charts = charts;
        _ruleEngine = ruleEngine;
        _accounts = accounts;
        _httpContextAccessor = httpContextAccessor;
        _promptBuilders = promptBuilders.ToDictionary(b => b.TemplateId);
    }

    public async Task<IActionResult> ExecuteBaziRead(int tier, BaziReadRequest req, CancellationToken cancellationToken = default)
    {
        if (!ValidTier(tier))
        {
            return new BadRequestObjectResult(new { error = "tier must be 0, 1, or 2" });
        }

        var credits = await EnsureCreditsAsync(tier, $"bazi-{req.Year}-{req.Month}-{req.Day}-{req.Hour}", cancellationToken);
        if (credits is not null)
        {
            return credits;
        }

        var input = LabBaziMapper.ToInput(req);
        var (chart, engineId) = _charts.CalculateBazi(input);
        var digest = ReadingSummaries.BuildBaziRuleDigest(chart, req.Focus, _ruleEngine);
        var preview = ReadingSummaries.BuildBaziTier0Preview(chart, req.Focus);

        if (tier == 0)
        {
            return OkEnvelope("bazi", tier, chart, digest, preview, null, engineId);
        }

        var result = _interpretation.Interpret(chart, req.Focus, req.MaxTokens ?? 512);
        return OkEnvelope("bazi", tier, chart, digest, preview, new
        {
            text = result.Text,
            textEn = result.TextEn,
            isFallback = result.IsFallback
        }, engineId, result.Engine);
    }

    public async Task<IActionResult> ExecuteLiuyaoRead(int tier, LiuyaoReadRequest req, CancellationToken cancellationToken = default)
    {
        if (!ValidTier(tier))
        {
            return new BadRequestObjectResult(new { error = "tier must be 0, 1, or 2" });
        }

        var credits = await EnsureCreditsAsync(tier, $"liuyao-{req.Method}-{req.Seed}", cancellationToken);
        if (credits is not null)
        {
            return credits;
        }

        var (chart, engineId) = _charts.CalculateLiuyao(req.Method, req.At ?? DateTimeOffset.Now, req.Seed);
        var digest = ReadingSummaries.BuildLiuyaoRuleDigest(chart, req.Question, req.Focus, _ruleEngine);
        var preview = ReadingSummaries.BuildLiuyaoTier0Preview(chart, req.Question, req.Focus);

        if (tier == 0)
        {
            return OkEnvelope("liuyao", tier, chart, digest, preview, null, engineId);
        }

        var liuyaoBuilder = ResolvePromptBuilder("liuyao-tier1-default");
        var liuyaoCtx = new PromptContext(
            Chart: chart,
            RuleDigest: digest,
            Question: req.Question ?? "综合",
            Focus: req.Focus,
            MaxTokens: req.MaxTokens ?? 512,
            Engine: _interpretation.ResolveEngineMetadata("liuyao", engineId));
        var prompt = liuyaoBuilder.Build(liuyaoCtx).PromptText;
        var gen = await _interpretation.GenerateWithFallbackAsync(
            prompt,
            new GenerateOptions(MaxTokens: req.MaxTokens ?? 512),
            CancellationToken.None);
        return OkEnvelope("liuyao", tier, chart, digest, preview, new
        {
            text = gen.IsFallback ? preview.OneLiner : gen.Text,
            isFallback = gen.IsFallback,
            fallbackReason = gen.FallbackReason
        }, engineId, gen.EngineId);
    }

    public async Task<IActionResult> ExecuteTarotRead(int tier, TarotReadRequest req, CancellationToken cancellationToken = default)
    {
        if (!ValidTier(tier))
        {
            return new BadRequestObjectResult(new { error = "tier must be 0, 1, or 2" });
        }

        var credits = await EnsureCreditsAsync(tier, $"tarot-{req.SpreadId}-{req.Seed}", cancellationToken);
        if (credits is not null)
        {
            return credits;
        }

        var (reading, engineId) = _charts.DrawTarotReading(req.SpreadId, req.Question, req.Seed);
        var digest = new
        {
            enriched = TarotReadingEnricher.BuildEnrichedRuleDigest(reading),
            rules = ReadingSummaries.BuildTarotRuleDigest(reading, _ruleEngine)
        };
        var preview = ReadingSummaries.BuildTarotTier0Preview(reading, req.Question);

        if (tier == 0)
        {
            return OkEnvelope("tarot", tier, reading, digest, preview, null, engineId);
        }

        var positions = reading.Positions
            .Select(p => new TarotPositionPrompt(p.PositionTitleZh, p.PositionContext, p.CardNameZh, p.Reversed, p.Meaning))
            .ToList();
        var (templateId, useTranslatePass, wordLimit, maxTokens) =
            ResolveTarotPrompt(engineId, tier, reading.SpreadId);
        var tarotBuilder = ResolvePromptBuilder(templateId);
        var tarotCtx = new PromptContext(
            Chart: new TarotPromptInput(reading.SpreadTitleZh, positions, wordLimit),
            RuleDigest: digest,
            Question: req.Question ?? "General reading",
            Focus: null,
            MaxTokens: req.MaxTokens ?? maxTokens,
            Engine: _interpretation.ResolveEngineMetadata("tarot", engineId));
        var prompt = tarotBuilder.Build(tarotCtx).PromptText;

        object narrative;
        var tokenBudget = req.MaxTokens ?? maxTokens;
        if (useTranslatePass)
        {
            var result = _interpretation.InterpretTarotEnglishThenChinese(prompt, tokenBudget, tokenBudget);
            narrative = new
            {
                text = string.IsNullOrWhiteSpace(result.TextZh) ? preview.OneLiner : result.TextZh,
                textEn = result.TextEn,
                isFallback = result.IsFallback,
                fallbackReason = result.FallbackReason,
                promptTemplate = templateId
            };
        }
        else
        {
            var gen = await _interpretation.GenerateWithFallbackAsync(
                prompt,
                new GenerateOptions(MaxTokens: tokenBudget),
                CancellationToken.None);
            narrative = new
            {
                text = string.IsNullOrWhiteSpace(gen.Text) ? preview.OneLiner : gen.Text,
                textEn = (string?)null,
                isFallback = gen.IsFallback,
                fallbackReason = gen.FallbackReason,
                promptTemplate = templateId
            };
        }

        return OkEnvelope("tarot", tier, reading, digest, preview, narrative, engineId, templateId);
    }

    private async Task<IActionResult?> EnsureCreditsAsync(int tier, string readingId, CancellationToken cancellationToken)
    {
        var auth = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        var result = await _accounts.TryConsumeAsync(auth, tier, readingId, cancellationToken);
        if (result.Allowed)
        {
            return null;
        }

        var status = result.Error?.Contains("Token", StringComparison.OrdinalIgnoreCase) == true ? 401 : 402;
        return new ObjectResult(new { error = result.Error }) { StatusCode = status };
    }

    private IPromptBuilder ResolvePromptBuilder(string templateId) =>
        _promptBuilders.TryGetValue(templateId, out var builder)
            ? builder
            : throw new InvalidOperationException($"prompt builder not registered: {templateId}");

    private static bool ValidTier(int tier) => tier is >= 0 and <= 2;

    private static IActionResult OkEnvelope(
        string domain,
        int tier,
        object chart,
        object? ruleDigest,
        Tier0Preview tier0Preview,
        object? narrative,
        string? paipanEngine,
        string? inferenceEngineId = null,
        string? promptTemplateId = null)
    {
        var engineId = inferenceEngineId ?? paipanEngine ?? domain;
        var output = narrative is null
            ? null
            : ReadingEnvelopeBuilder.NarrativeToOutput(domain, narrative, engineId);
        if (output is not null && promptTemplateId is not null)
        {
            output = output with { PromptTemplateId = promptTemplateId };
        }

        var envelope = ReadingEnvelopeBuilder.Build(
            domain,
            tier,
            chart,
            ruleDigest,
            tier0Preview,
            output,
            paipanEngine,
            promptTemplateId: promptTemplateId);
        return new OkObjectResult(envelope);
    }

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
