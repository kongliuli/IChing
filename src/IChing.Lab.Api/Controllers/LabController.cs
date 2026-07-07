using System.Text.Json;
using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Abstractions.Prompts;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Calendar;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Rules;
using IChing.Lab.Core.Services;
using IChing.Lab.Core.Tarot;
using IChing.Lab.Engines.Tarot;
using IChing.Lab.Inference;
using IChing.Lab.Inference.Prompts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace IChing.Lab.Api.Controllers;

[ApiController]
[Route("lab")]
public class LabController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ChartInterpretationOrchestrator _interpretation;
    private readonly ChartEngineRouter _chartRouter;
    private readonly IEnumerable<IChartEngine> _engines;
    private readonly IEnumerable<IInferenceEngine> _inferenceEngines;
    private readonly IReadOnlyDictionary<string, IPromptBuilder> _promptBuilders;
    private readonly IConfiguration _configuration;
    private readonly RuleEngine _ruleEngine;

    public LabController(
        ChartInterpretationOrchestrator interpretation,
        ChartEngineRouter chartRouter,
        IEnumerable<IChartEngine> engines,
        IEnumerable<IPromptBuilder> promptBuilders,
        IEnumerable<IInferenceEngine> inferenceEngines,
        IConfiguration configuration,
        RuleEngine ruleEngine)
    {
        _interpretation = interpretation;
        _chartRouter = chartRouter;
        _engines = engines;
        _inferenceEngines = inferenceEngines;
        _promptBuilders = promptBuilders.ToDictionary(b => b.TemplateId);
        _configuration = configuration;
        _ruleEngine = ruleEngine;
    }

    /// <summary>按 templateId 选取已注册的 IPromptBuilder。</summary>
    private IPromptBuilder ResolvePromptBuilder(string templateId) =>
        _promptBuilders.TryGetValue(templateId, out var builder)
            ? builder
            : throw new InvalidOperationException($"prompt builder not registered: {templateId}");

    /// <summary>
    /// 列出所有已注册的排盘引擎，返回完整 metadata：domain / engineId / source / version /
    /// algorithmBasis / templateHint / moduleFocus，供客户端按算法来源选取引擎与运维诊断使用。
    /// </summary>
    [HttpGet("engines")]
    public IActionResult Engines() =>
        Ok(_engines.Select(e => new
        {
            domain = e.Domain,
            engineId = e.EngineId,
            source = e.Metadata.Source,
            version = e.Metadata.Version,
            algorithmBasis = e.Metadata.AlgorithmBasis,
            templateHint = e.Metadata.TemplateHint,
            moduleFocus = e.Metadata.ModuleFocus
        }));

    /// <summary>简单存活探活，无需鉴权，返回 ok。</summary>
    [HttpGet("/health")]
    public IActionResult Health() => Ok(new { status = "ok" });

    /// <summary>
    /// 解读引擎健康检查：返回所有已注册 IInferenceEngine 的就绪状态与是否默认引擎。
    /// 路由以 "/" 开头为绝对路径，绕过控制器级 <c>lab</c> 前缀，落在 <c>/health/engines</c>。
    /// 不需要鉴权，供内部探活与运维面板使用。
    /// </summary>
    [HttpGet("/health/engines")]
    public IActionResult HealthEngines()
    {
        var defaultEngineId = ResolveDefaultEngineId();
        var payload = _inferenceEngines
            .Select(e => new EngineHealthStatus(e.EngineId, e.IsReady, e.EngineId == defaultEngineId))
            .ToList();
        return Ok(payload);
    }

    /// <summary>排盘引擎探活：对各引擎执行最小 Calculate，返回 ready 状态。</summary>
    [HttpGet("/health/chart-engines")]
    public IActionResult HealthChartEngines()
    {
        var payload = _engines.Select(e => new ChartEngineHealthStatus(
            e.Domain,
            e.EngineId,
            ProbeChartEngineReady(e),
            e.EngineId == ResolveChartEngine(_configuration, e.Domain))).ToList();
        return Ok(payload);
    }

    private static bool ProbeChartEngineReady(IChartEngine engine)
    {
        var args = engine.Domain switch
        {
            "bazi" => new Dictionary<string, object?>
            {
                ["year"] = 1990, ["month"] = 5, ["day"] = 20, ["hour"] = 10, ["gender"] = 1
            },
            "liuyao" => new Dictionary<string, object?> { ["method"] = "coin", ["seed"] = 1 },
            "tarot" => new Dictionary<string, object?> { ["spreadId"] = "single-card", ["seed"] = 1 },
            "calendar" => new Dictionary<string, object?> { ["year"] = 2026, ["month"] = 1, ["day"] = 1 },
            _ => new Dictionary<string, object?>()
        };

        try
        {
            var result = engine.Calculate(new ChartRequest(engine.Domain, args));
            return !ChartEngineRouter.IsErrorResult(result);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 解析默认引擎标识：优先读 <c>plugins:inferenceEngines</c> 中 <c>default=true</c> 的项，
    /// 其次读 <c>plugins:defaultEngine</c>；均未配置返回 null（所有引擎 isDefault=false）。
    /// </summary>
    private string? ResolveDefaultEngineId()
    {
        foreach (var child in _configuration.GetSection("plugins:inferenceEngines").GetChildren())
        {
            if (string.Equals(child["default"], "true", StringComparison.OrdinalIgnoreCase))
            {
                return child["id"];
            }
        }

        return _configuration["plugins:defaultEngine"];
    }

    /// <summary>
    /// 从 <c>plugins:chartEngines</c> 数组中查找 <c>domain</c> 等于指定域的项，
    /// 返回其 <c>default</c> 引擎标识；未配置或未匹配返回 null（由调用方回退到原硬编码逻辑）。
    /// 内部 static 以便单元测试在不构造控制器的情况下直接验证查找逻辑。
    /// </summary>
    internal static string? ResolveChartEngine(IConfiguration configuration, string domain)
    {
        foreach (var child in configuration.GetSection("plugins:chartEngines").GetChildren())
        {
            if (string.Equals(child["domain"], domain, StringComparison.OrdinalIgnoreCase))
            {
                return child["default"];
            }
        }

        return null;
    }

    [HttpPost("bazi")]
    public IActionResult Bazi([FromBody] BaziRequest req)
    {
        var input = MapBaziInput(req);
        var (chart, engineId) = CalculateBazi(input);
        return Ok(new { chart, engine = new { paipan = engineId } });
    }

    [HttpPost("bazi/read")]
    public IActionResult BaziRead([FromQuery] int tier, [FromBody] BaziReadRequest req) =>
        ExecuteBaziRead(tier, req);

    /// <summary>三域统一 Tier 入口：bazi / liuyao / tarot。</summary>
    [HttpPost("{domain}/read")]
    public async Task<IActionResult> UnifiedRead(string domain, [FromQuery] int tier, [FromBody] JsonElement body)
    {
        switch (domain.ToLowerInvariant())
        {
            case "bazi":
                var baziReq = body.Deserialize<BaziReadRequest>(JsonOptions);
                return baziReq is null
                    ? BadRequest(new { error = "invalid bazi request body" })
                    : ExecuteBaziRead(tier, baziReq);
            case "liuyao":
                var liuyaoReq = body.Deserialize<LiuyaoReadRequest>(JsonOptions);
                return liuyaoReq is null
                    ? BadRequest(new { error = "invalid liuyao request body" })
                    : await ExecuteLiuyaoRead(tier, liuyaoReq);
            case "tarot":
                var tarotReq = body.Deserialize<TarotReadRequest>(JsonOptions);
                return tarotReq is null
                    ? BadRequest(new { error = "invalid tarot request body" })
                    : await ExecuteTarotRead(tier, tarotReq);
            default:
                return NotFound(new
                {
                    error = $"unknown domain: {domain}",
                    supported = new[] { "bazi", "liuyao", "tarot" }
                });
        }
    }

    private IActionResult ExecuteBaziRead(int tier, BaziReadRequest req)
    {
        if (!ValidTier(tier))
        {
            return BadRequest(new { error = "tier must be 0, 1, or 2" });
        }

        var input = MapBaziInput(req);
        var (chart, engineId) = CalculateBazi(input);
        var digest = ReadingSummaries.BuildBaziRuleDigest(chart, req.Focus, _ruleEngine);
        var preview = ReadingSummaries.BuildBaziTier0Preview(chart, req.Focus);

        if (tier == 0)
        {
            return Ok(ReadEnvelope("bazi", tier, chart, digest, preview, null, engineId));
        }

        var result = _interpretation.Interpret(chart, req.Focus, req.MaxTokens ?? 512);
        return Ok(ReadEnvelope("bazi", tier, chart, digest, preview, new
        {
            text = result.Text,
            textEn = result.TextEn,
            isFallback = result.IsFallback
        }, engineId));
    }

    [HttpPost("bazi/interpret")]
    public IActionResult BaziInterpret([FromBody] BaziInterpretRequest req)
    {
        var input = MapBaziInput(req);
        var (chart, engineId) = CalculateBazi(input);
        var result = _interpretation.Interpret(chart, req.Focus, req.MaxTokens ?? 256);
        return Ok(new { chart, interpretation = result, engine = new { paipan = engineId } });
    }

    [HttpPost("bazi/hepan")]
    public IActionResult HePan([FromBody] HePanRequest req)
    {
        var inputA = MapBaziInput(req.PersonA);
        var inputB = MapBaziInput(req.PersonB);
        var (a, engineA) = CalculateBazi(inputA);
        var (b, engineB) = CalculateBazi(inputB);
        return Ok(new
        {
            comparison = HePanService.Compare(a, b),
            engine = new { paipanA = engineA, paipanB = engineB }
        });
    }

    [HttpGet("bazi/cities")]
    public IActionResult Cities() => Ok(CityLookup.List());

    [HttpPost("interpret")]
    public IActionResult Interpret([FromBody] InterpretRequest req)
    {
        var result = _interpretation.Interpret(req.Chart, req.Focus, req.MaxTokens ?? 256);
        return Ok(result);
    }

    [HttpGet("interpret/status")]
    public IActionResult InterpretStatus() =>
        Ok(new { loaded = _interpretation.IsModelLoaded });

    [HttpPost("liuyao/coin")]
    public IActionResult LiuyaoCoin([FromQuery] int? seed)
    {
        var (chart, engineId) = CalculateLiuyao("coin", DateTimeOffset.Now, seed);
        return Ok(new { chart, engine = new { paipan = engineId } });
    }

    [HttpPost("liuyao/time")]
    public IActionResult LiuyaoTime([FromQuery] DateTime? at)
    {
        var when = at.HasValue ? new DateTimeOffset(at.Value) : DateTimeOffset.Now;
        var (chart, engineId) = CalculateLiuyao("time", when, null);
        return Ok(new { chart, engine = new { paipan = engineId } });
    }

    [HttpPost("liuyao/read")]
    public Task<IActionResult> LiuyaoRead([FromQuery] int tier, [FromBody] LiuyaoReadRequest req) =>
        ExecuteLiuyaoRead(tier, req);

    private async Task<IActionResult> ExecuteLiuyaoRead(int tier, LiuyaoReadRequest req)
    {
        if (!ValidTier(tier))
        {
            return BadRequest(new { error = "tier must be 0, 1, or 2" });
        }

        var (chart, engineId) = CalculateLiuyao(req.Method, req.At ?? DateTimeOffset.Now, req.Seed);
        var digest = ReadingSummaries.BuildLiuyaoRuleDigest(chart, req.Question, req.Focus, _ruleEngine);
        var preview = ReadingSummaries.BuildLiuyaoTier0Preview(chart, req.Question, req.Focus);

        if (tier == 0)
        {
            return Ok(ReadEnvelope("liuyao", tier, chart, digest, preview, null, engineId));
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
        return Ok(ReadEnvelope("liuyao", tier, chart, digest, preview, new
        {
            text = gen.IsFallback ? preview.OneLiner : gen.Text,
            isFallback = gen.IsFallback,
            fallbackReason = gen.FallbackReason
        }, engineId));
    }

    [HttpPost("tarot/draw")]
    public IActionResult TarotDraw([FromBody] TarotDrawRequest req)
    {
        var (reading, engineId) = DrawTarotReading(req.SpreadId, req.Question, req.Seed);
        return Ok(new { reading, engine = new { paipan = engineId } });
    }

    [HttpPost("tarot/read")]
    public Task<IActionResult> TarotRead([FromQuery] int tier, [FromBody] TarotReadRequest req) =>
        ExecuteTarotRead(tier, req);

    private async Task<IActionResult> ExecuteTarotRead(int tier, TarotReadRequest req)
    {
        if (!ValidTier(tier))
        {
            return BadRequest(new { error = "tier must be 0, 1, or 2" });
        }

        var (reading, engineId) = DrawTarotReading(req.SpreadId, req.Question, req.Seed);
        var digest = new
        {
            enriched = TarotReadingEnricher.BuildEnrichedRuleDigest(reading),
            rules = ReadingSummaries.BuildTarotRuleDigest(reading, _ruleEngine)
        };
        var preview = ReadingSummaries.BuildTarotTier0Preview(reading, req.Question);

        if (tier == 0)
        {
            return Ok(ReadEnvelope("tarot", tier, reading, digest, preview, null, engineId));
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

        return Ok(ReadEnvelope("tarot", tier, reading, digest, preview, narrative, engineId));
    }

    [HttpPost("tarot/interpret")]
    public IActionResult TarotInterpret([FromBody] TarotDrawRequest req)
    {
        var (reading, engineId) = DrawTarotReading(req.SpreadId, req.Question, req.Seed);
        var narrative = TarotNarrative.Build(reading);
        return Ok(new { reading, narrative, engine = new { paipan = engineId } });
    }

    [HttpGet("tarot/spreads")]
    public IActionResult TarotSpreads() => Ok(SpreadCatalog.List());

    [HttpGet("calendar/day")]
    public IActionResult CalendarDay(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] int day,
        [FromQuery] int sect = 1)
    {
        var (huangLi, engineId) = CalculateCalendar(year, month, day, sect);
        return Ok(new { day = huangLi, engine = new { paipan = engineId } });
    }

    private static bool ValidTier(int tier) => tier is >= 0 and <= 2;

    private static object ReadEnvelope(string domain, int tier, object chart, object? ruleDigest, Tier0Preview tier0Preview, object? narrative, string? paipanEngine = null) => new
    {
        domain,
        tier,
        engine = new
        {
            paipan = paipanEngine ?? (domain == "liuyao" ? "IChingLibrary.SixLines" : "IChing.Lab.Core"),
            rules = "iching-rules-v1",
            narrative = tier == 0 ? "none" : "qwen2.5-1.5b-onnx-genai"
        },
        chart,
        ruleDigest,
        tier0Preview = new { oneLiner = tier0Preview.OneLiner, disclaimer = tier0Preview.Disclaimer },
        narrative
    };

    private (BaziChart Chart, string EngineId) CalculateBazi(BaziInput input)
    {
        var chain = ChartEngineRouter.ResolveEngineChain(_configuration, "bazi", "lunar-csharp-1.6.8");
        var routed = _chartRouter.Calculate("bazi", ToArgs(input), chain);
        return (ChartResultMapper.AsBaziChart(routed.Result, input), routed.EngineId);
    }

    private (LiuyaoNajiaResult Chart, string EngineId) CalculateLiuyao(
        string? method,
        DateTimeOffset at,
        int? seed)
    {
        var args = new Dictionary<string, object?>
        {
            ["method"] = method ?? "coin",
            ["at"] = at,
            ["seed"] = seed
        };
        var chain = ChartEngineRouter.ResolveEngineChain(_configuration, "liuyao", "iching-sixlines-2.0.3");
        var routed = _chartRouter.Calculate("liuyao", args, chain);
        return (ChartResultMapper.AsLiuyaoChart(routed.Result, method, at, seed), routed.EngineId);
    }

    private (HuangLiDay Day, string EngineId) CalculateCalendar(int year, int month, int day, int sect)
    {
        var args = new Dictionary<string, object?>
        {
            ["year"] = year,
            ["month"] = month,
            ["day"] = day,
            ["sect"] = sect
        };
        var chain = ChartEngineRouter.ResolveEngineChain(_configuration, "calendar", "lunar-csharp-1.6.8");
        var routed = _chartRouter.Calculate("calendar", args, chain);
        return (ChartResultMapper.AsCalendarDay(routed.Result, year, month, day, sect), routed.EngineId);
    }

    private static Dictionary<string, object?> ToArgs<T>(T input)
    {
        var json = JsonSerializer.Serialize(input, JsonOptions);
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(json)
            ?? new Dictionary<string, object?>();
    }

    private (TarotReading Reading, string EngineId) DrawTarotReading(string? spreadId, string? question, int? seed)
    {
        var chain = ChartEngineRouter.ResolveEngineChain(_configuration, "tarot", "iching-tarot-built-in");
        return TarotDrawPipeline.Draw(_chartRouter, chain, spreadId, question, seed);
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

    private static object BuildTarotRuleDigest(TarotReading reading) =>
        TarotReadingEnricher.BuildEnrichedRuleDigest(reading);

    private static BaziInput MapBaziInput(BaziRequest req) =>
        new(req.Year, req.Month, req.Day, req.Hour, req.Minute, req.Second,
            req.Longitude, req.City, req.Gender, req.Sect,
            req.FlowYear, req.FlowMonth, req.FlowCalendarMonth, req.FlowDay);

    private static BaziInput MapBaziInput(BaziReadRequest req) =>
        new(req.Year, req.Month, req.Day, req.Hour, req.Minute, req.Second,
            req.Longitude, req.City, req.Gender, req.Sect,
            req.FlowYear, req.FlowMonth, req.FlowCalendarMonth, req.FlowDay);

    private static BaziInput MapBaziInput(BaziInterpretRequest req) =>
        new(req.Year, req.Month, req.Day, req.Hour, req.Minute, req.Second,
            req.Longitude, req.City, req.Gender, req.Sect,
            req.FlowYear, req.FlowMonth, req.FlowCalendarMonth, req.FlowDay);
}

public record BaziRequest(
    int Year, int Month, int Day, int Hour,
    int Minute = 0, int Second = 0,
    double? Longitude = null,
    string? City = null,
    int? Gender = null,
    int Sect = 1,
    int? FlowYear = null,
    int? FlowMonth = null,
    int? FlowCalendarMonth = null,
    int? FlowDay = null);

public record BaziReadRequest(
    int Year, int Month, int Day, int Hour,
    int Minute = 0, int Second = 0,
    double? Longitude = null,
    string? City = null,
    int? Gender = null,
    int Sect = 1,
    int? FlowYear = null,
    int? FlowMonth = null,
    int? FlowCalendarMonth = null,
    int? FlowDay = null,
    string? Focus = null,
    int? MaxTokens = null);

public record BaziInterpretRequest(
    int Year, int Month, int Day, int Hour,
    int Minute = 0, int Second = 0,
    double? Longitude = null,
    string? City = null,
    int? Gender = null,
    int Sect = 1,
    int? FlowYear = null,
    int? FlowMonth = null,
    int? FlowCalendarMonth = null,
    int? FlowDay = null,
    string? Focus = null,
    int? MaxTokens = null);

public record HePanRequest(BaziRequest PersonA, BaziRequest PersonB);

public record InterpretRequest(object Chart, string? Focus, int? MaxTokens);

public record TarotDrawRequest(string? SpreadId, string? Question, int? Seed);

public record LiuyaoReadRequest(
    string? Method,
    DateTimeOffset? At,
    int? Seed,
    string? Question,
    string? Focus,
    int? MaxTokens);

public record TarotReadRequest(string? SpreadId, string? Question, int? Seed, int? MaxTokens);

/// <summary>
/// 解读引擎健康状态项，序列化为 <c>{ engineId, isReady, isDefault }</c>。
/// </summary>
public record EngineHealthStatus(string EngineId, bool IsReady, bool IsDefault);

public record ChartEngineHealthStatus(string Domain, string EngineId, bool IsReady, bool IsDefault);
