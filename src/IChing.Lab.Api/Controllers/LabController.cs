using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Calendar;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Tarot;
using IChing.Lab.Inference;
using IChing.Lab.Inference.Prompts;
using Microsoft.AspNetCore.Mvc;

namespace IChing.Lab.Api.Controllers;

[ApiController]
[Route("lab")]
public class LabController : ControllerBase
{
    private readonly ChartInterpretationService _interpretation;

    public LabController(ChartInterpretationService interpretation)
    {
        _interpretation = interpretation;
    }

    [HttpPost("bazi")]
    public IActionResult Bazi([FromBody] BaziRequest req)
    {
        var chart = BaziEngine.Calculate(MapBaziInput(req));
        return Ok(chart);
    }

    [HttpPost("bazi/read")]
    public IActionResult BaziRead([FromQuery] int tier, [FromBody] BaziReadRequest req)
    {
        if (!ValidTier(tier))
        {
            return BadRequest(new { error = "tier must be 0, 1, or 2" });
        }

        var chart = BaziEngine.Calculate(MapBaziInput(req));
        var preview = new { oneLiner = $"日柱 {chart.DayPillar.GanZhi}，月柱 {chart.MonthPillar.GanZhi}；先看四柱、大运与关注点「{req.Focus ?? "综合"}」。" };

        if (tier == 0)
        {
            return Ok(ReadEnvelope("bazi", tier, chart, null, preview, null));
        }

        var result = _interpretation.Interpret(chart, req.Focus, req.MaxTokens ?? 512);
        return Ok(ReadEnvelope("bazi", tier, chart, null, preview, new
        {
            text = result.Text,
            textEn = result.TextEn,
            isFallback = result.IsFallback
        }));
    }

    [HttpPost("bazi/interpret")]
    public IActionResult BaziInterpret([FromBody] BaziInterpretRequest req)
    {
        var chart = BaziEngine.Calculate(MapBaziInput(req));
        var result = _interpretation.Interpret(chart, req.Focus, req.MaxTokens ?? 256);
        return Ok(new { chart, interpretation = result });
    }

    [HttpPost("bazi/hepan")]
    public IActionResult HePan([FromBody] HePanRequest req)
    {
        var a = BaziEngine.Calculate(MapBaziInput(req.PersonA));
        var b = BaziEngine.Calculate(MapBaziInput(req.PersonB));
        return Ok(HePanService.Compare(a, b));
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
    public IActionResult LiuyaoCoin([FromQuery] int? seed) =>
        Ok(LiuyaoNajiaService.Coin(DateTimeOffset.Now, seed));

    [HttpPost("liuyao/time")]
    public IActionResult LiuyaoTime([FromQuery] DateTime? at) =>
        Ok(LiuyaoNajiaService.Time(at ?? DateTimeOffset.Now.DateTime));

    [HttpPost("liuyao/read")]
    public IActionResult LiuyaoRead([FromQuery] int tier, [FromBody] LiuyaoReadRequest req)
    {
        if (!ValidTier(tier))
        {
            return BadRequest(new { error = "tier must be 0, 1, or 2" });
        }

        var chart = req.Method == "time"
            ? LiuyaoNajiaService.Time(req.At ?? DateTimeOffset.Now)
            : LiuyaoNajiaService.Coin(req.At ?? DateTimeOffset.Now, req.Seed);
        var digest = BuildLiuyaoRuleDigest(chart, req.Question, req.Focus);
        var preview = new
        {
            oneLiner = $"{chart.OriginalHexagram}{(chart.ChangedHexagram is null ? "" : $" 之 {chart.ChangedHexagram}")}；{string.Join("，", chart.Lines.Where(l => l.IsChanging).Select(l => $"{l.Index}爻动"))}"
        };

        if (tier == 0)
        {
            return Ok(ReadEnvelope("liuyao", tier, chart, digest, preview, null));
        }

        var prompt = LiuyaoPromptBuilder.BuildTier1(req.Question ?? "综合", req.Focus, digest, chart);
        var gen = _interpretation.Generate(prompt, req.MaxTokens ?? 512);
        return Ok(ReadEnvelope("liuyao", tier, chart, digest, preview, new
        {
            text = gen.IsFallback ? preview.oneLiner : gen.Text,
            isFallback = gen.IsFallback,
            fallbackReason = gen.FallbackReason
        }));
    }

    [HttpPost("tarot/draw")]
    public IActionResult TarotDraw([FromBody] TarotDrawRequest req) =>
        Ok(TarotEngine.Draw(req.SpreadId ?? "past-present-future", req.Question, req.Seed));

    [HttpPost("tarot/read")]
    public IActionResult TarotRead([FromQuery] int tier, [FromBody] TarotReadRequest req)
    {
        if (!ValidTier(tier))
        {
            return BadRequest(new { error = "tier must be 0, 1, or 2" });
        }

        var reading = TarotEngine.Draw(req.SpreadId ?? "past-present-future", req.Question, req.Seed);
        var digest = BuildTarotRuleDigest(reading);
        var preview = new
        {
            oneLiner = string.Join("；", reading.Positions.Select(p => $"[{p.PositionTitleZh}] {p.CardNameZh}{(p.Reversed ? "逆位" : "正位")}：{p.Meaning}"))
        };

        if (tier == 0)
        {
            return Ok(ReadEnvelope("tarot", tier, reading, digest, preview, null));
        }

        var positions = reading.Positions
            .Select(p => new TarotPositionPrompt(p.PositionTitleZh, p.PositionContext, p.CardNameZh, p.Reversed, p.Meaning))
            .ToList();
        var prompt = TarotPromptBuilder.BuildEnglishTier1(
            req.Question ?? "General reading",
            reading.SpreadTitleZh,
            digest,
            positions,
            reading.Positions.Count >= 10 ? 500 : 280);
        var result = _interpretation.InterpretTarotEnglishThenChinese(prompt, req.MaxTokens ?? 512, req.MaxTokens ?? 512);

        return Ok(ReadEnvelope("tarot", tier, reading, digest, preview, new
        {
            text = string.IsNullOrWhiteSpace(result.TextZh) ? preview.oneLiner : result.TextZh,
            textEn = result.TextEn,
            isFallback = result.IsFallback,
            fallbackReason = result.FallbackReason
        }));
    }

    [HttpPost("tarot/interpret")]
    public IActionResult TarotInterpret([FromBody] TarotDrawRequest req)
    {
        var reading = TarotEngine.Draw(req.SpreadId ?? "past-present-future", req.Question, req.Seed);
        var narrative = TarotNarrative.Build(reading);
        return Ok(new { reading, narrative });
    }

    [HttpGet("tarot/spreads")]
    public IActionResult TarotSpreads() => Ok(SpreadCatalog.List());

    [HttpGet("calendar/day")]
    public IActionResult CalendarDay(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] int day,
        [FromQuery] int sect = 1) =>
        Ok(HuangLiService.GetDay(year, month, day, sect));

    private static bool ValidTier(int tier) => tier is >= 0 and <= 2;

    private static object ReadEnvelope(string domain, int tier, object chart, object? ruleDigest, object tier0Preview, object? narrative) => new
    {
        domain,
        tier,
        engine = new
        {
            paipan = domain == "liuyao" ? "IChingLibrary.SixLines" : "IChing.Lab.Core",
            rules = "iching-rules-v0",
            narrative = tier == 0 ? "none" : "qwen2.5-1.5b-onnx-genai"
        },
        chart,
        ruleDigest,
        tier0Preview,
        narrative
    };

    private static object BuildLiuyaoRuleDigest(LiuyaoNajiaResult chart, string? question, string? focus)
    {
        var shi = chart.Lines.FirstOrDefault(l => l.Role?.Contains("世") == true);
        var ying = chart.Lines.FirstOrDefault(l => l.Role?.Contains("应") == true);
        return new
        {
            shiYaoSummary = shi is null ? "未标出世爻" : $"{shi.Index}爻{shi.SixKin}{shi.StemBranch}持世",
            yingYaoSummary = ying is null ? "未标出应爻" : $"{ying.Index}爻{ying.SixKin}{ying.StemBranch}为应",
            changingSummaries = chart.Lines.Where(l => l.IsChanging).Select(l => $"{l.Index}爻{l.SixKin}{l.StemBranch}动").ToList(),
            questionType = focus ?? question ?? "综合",
            yongShenSummary = "未分类问事默认以世爻为用神",
            alerts = Array.Empty<string>()
        };
    }

    private static object BuildTarotRuleDigest(TarotReading reading)
    {
        var names = reading.Positions.Select(p => p.CardName).ToList();
        return new
        {
            majorCount = names.Count(n => !n.Contains(" of ")),
            total = names.Count,
            wands = names.Count(n => n.EndsWith("of Wands")),
            cups = names.Count(n => n.EndsWith("of Cups")),
            swords = names.Count(n => n.EndsWith("of Swords")),
            pentacles = names.Count(n => n.EndsWith("of Pentacles")),
            reversedCount = reading.Positions.Count(p => p.Reversed)
        };
    }

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
