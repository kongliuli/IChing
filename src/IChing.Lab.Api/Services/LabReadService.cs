using IChing.Lab.Abstractions.Models;
using IChing.Lab.Abstractions.Prompts;
using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Api.Contracts;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Readings.Templates;
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
        var exchangeInput = ExchangeInputBuilder.ForBazi(chart, digest, req.Focus);

        if (tier == 0)
        {
            return OkEnvelope("bazi", tier, chart, exchangeInput, preview, null, engineId);
        }

        var template = ReadingTemplateRegistry.ResolveInitial("bazi", tier);
        var exchange = ReadingExchangeFactory.CreateInitial("bazi", tier, exchangeInput, null, template.TemplateId);
        var ctx = ExchangeInferenceRouter.BuildInitialContext(
            exchange,
            chart,
            digest,
            _interpretation.ResolveEngineMetadata("bazi", engineId),
            _interpretation.ResolveModuleFocuses("bazi", engineId),
            req.MaxTokens ?? 512);
        var gen = await RunTemplateInferenceAsync(
            "bazi",
            template.TemplateId,
            ctx,
            req.MaxTokens ?? 512,
            cancellationToken);
        return OkEnvelope("bazi", tier, chart, exchangeInput, preview, new
        {
            text = gen.IsFallback ? preview.OneLiner : gen.Text,
            isFallback = gen.IsFallback,
            fallbackReason = gen.FallbackReason
        }, engineId, gen.EngineId, template.TemplateId);
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
        var exchangeInput = ExchangeInputBuilder.ForLiuyao(chart, digest, req.Question, req.Focus);

        if (tier == 0)
        {
            return OkEnvelope("liuyao", tier, chart, exchangeInput, preview, null, engineId);
        }

        var template = ReadingTemplateRegistry.ResolveInitial("liuyao", tier);
        var exchange = ReadingExchangeFactory.CreateInitial("liuyao", tier, exchangeInput, null, template.TemplateId);
        var ctx = ExchangeInferenceRouter.BuildInitialContext(
            exchange,
            chart,
            digest,
            _interpretation.ResolveEngineMetadata("liuyao", engineId),
            null,
            req.MaxTokens ?? 512);
        var gen = await RunTemplateInferenceAsync(
            "liuyao",
            template.TemplateId,
            ctx,
            req.MaxTokens ?? 512,
            cancellationToken);
        return OkEnvelope("liuyao", tier, chart, exchangeInput, preview, new
        {
            text = gen.IsFallback ? preview.OneLiner : gen.Text,
            isFallback = gen.IsFallback,
            fallbackReason = gen.FallbackReason
        }, engineId, gen.EngineId, template.TemplateId);
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
        var exchangeInput = ExchangeInputBuilder.ForTarot(reading, req.Question);

        if (tier == 0)
        {
            return OkEnvelope("tarot", tier, reading, exchangeInput, preview, null, engineId);
        }

        var positions = reading.Positions
            .Select(p => new TarotPositionPrompt(p.PositionTitleZh, p.PositionContext, p.CardNameZh, p.Reversed, p.Meaning))
            .ToList();
        var tarotResolution = ReadingTemplateRegistry.ResolveTarot(engineId, tier, reading.SpreadId);
        var templateId = tarotResolution.Descriptor.TemplateId;
        var tarotBuilder = ResolvePromptBuilder(templateId);
        var tarotCtx = new PromptContext(
            Chart: new TarotPromptInput(reading.SpreadTitleZh, positions, tarotResolution.WordLimit),
            RuleDigest: digest,
            Question: req.Question ?? "General reading",
            Focus: null,
            MaxTokens: req.MaxTokens ?? tarotResolution.MaxTokens,
            Engine: _interpretation.ResolveEngineMetadata("tarot", engineId));
        var prompt = ReadingJsonOutputContract.Append(
            "tarot",
            tarotBuilder.Build(tarotCtx).PromptText,
            templateId);

        object narrative;
        var tokenBudget = req.MaxTokens ?? tarotResolution.MaxTokens;
        if (tarotResolution.Descriptor.NeedsTranslationPass)
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
                cancellationToken);
            narrative = new
            {
                text = string.IsNullOrWhiteSpace(gen.Text) ? preview.OneLiner : gen.Text,
                textEn = (string?)null,
                isFallback = gen.IsFallback,
                fallbackReason = gen.FallbackReason,
                promptTemplate = templateId
            };
        }

        return OkEnvelope("tarot", tier, reading, exchangeInput, preview, narrative, engineId, templateId);
    }

    private async Task<GenerationResult> RunTemplateInferenceAsync(
        string domain,
        string templateId,
        PromptContext ctx,
        int maxTokens,
        CancellationToken cancellationToken)
    {
        var builder = ResolvePromptBuilder(templateId);
        var prompt = ReadingJsonOutputContract.Append(domain, builder.Build(ctx).PromptText, templateId);
        return await _interpretation.GenerateWithFallbackAsync(
            prompt,
            new GenerateOptions(MaxTokens: maxTokens),
            cancellationToken);
    }

    public async Task<AccountsConsumeResult> ConsumeCreditsAsync(
        string? authorizationHeader,
        int tier,
        string exchangeId,
        CancellationToken cancellationToken) =>
        await _accounts.TryConsumeAsync(authorizationHeader, tier, exchangeId, cancellationToken);

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
        ExchangeInput exchangeInput,
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
            exchangeInput,
            tier0Preview,
            output,
            paipanEngine,
            promptTemplateId: promptTemplateId);
        return new OkObjectResult(envelope);
    }
}
