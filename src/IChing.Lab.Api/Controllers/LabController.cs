using System.Text.Json;
using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Prompts;
using IChing.Lab.Api.Contracts;
using IChing.Lab.Api.Services;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Tarot;
using IChing.Lab.Inference;
using Microsoft.AspNetCore.Mvc;

namespace IChing.Lab.Api.Controllers;

[ApiController]
[Route("lab")]
public class LabController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ChartInterpretationOrchestrator _interpretation;
    private readonly IEnumerable<IChartEngine> _engines;
    private readonly LabChartQueryService _charts;
    private readonly LabReadService _reads;
    private readonly LabHealthService _health;

    public LabController(
        ChartInterpretationOrchestrator interpretation,
        IEnumerable<IChartEngine> engines,
        LabChartQueryService charts,
        LabReadService reads,
        LabHealthService health)
    {
        _interpretation = interpretation;
        _engines = engines;
        _charts = charts;
        _reads = reads;
        _health = health;
    }

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

    [HttpGet("/health")]
    public IActionResult Health() => Ok(new { status = "ok" });

    [HttpGet("/health/engines")]
    public IActionResult HealthEngines() => Ok(_health.GetInferenceEngineHealth());

    [HttpGet("/health/chart-engines")]
    public IActionResult HealthChartEngines() => Ok(_health.GetChartEngineHealth());

    [HttpPost("bazi")]
    public IActionResult Bazi([FromBody] BaziRequest req)
    {
        var (chart, engineId) = _charts.CalculateBazi(LabBaziMapper.ToInput(req));
        return Ok(new { chart, engine = new { paipan = engineId } });
    }

    [HttpPost("bazi/read")]
    public Task<IActionResult> BaziRead([FromQuery] int tier, [FromBody] BaziReadRequest req) =>
        _reads.ExecuteBaziRead(tier, req);

    [HttpPost("{domain}/read")]
    public async Task<IActionResult> UnifiedRead(string domain, [FromQuery] int tier, [FromBody] JsonElement body)
    {
        switch (domain.ToLowerInvariant())
        {
            case "bazi":
                var baziReq = body.Deserialize<BaziReadRequest>(JsonOptions);
                return baziReq is null
                    ? BadRequest(new { error = "invalid bazi request body" })
                    : await _reads.ExecuteBaziRead(tier, baziReq);
            case "liuyao":
                var liuyaoReq = body.Deserialize<LiuyaoReadRequest>(JsonOptions);
                return liuyaoReq is null
                    ? BadRequest(new { error = "invalid liuyao request body" })
                    : await _reads.ExecuteLiuyaoRead(tier, liuyaoReq);
            case "tarot":
                var tarotReq = body.Deserialize<TarotReadRequest>(JsonOptions);
                return tarotReq is null
                    ? BadRequest(new { error = "invalid tarot request body" })
                    : await _reads.ExecuteTarotRead(tier, tarotReq);
            default:
                return NotFound(new
                {
                    error = $"unknown domain: {domain}",
                    supported = new[] { "bazi", "liuyao", "tarot" }
                });
        }
    }

    [HttpPost("bazi/interpret")]
    public IActionResult BaziInterpret([FromBody] BaziInterpretRequest req)
    {
        var (chart, engineId) = _charts.CalculateBazi(LabBaziMapper.ToInput(req));
        var result = _interpretation.Interpret(chart, req.Focus, req.MaxTokens ?? 256);
        return Ok(new { chart, interpretation = result, engine = new { paipan = engineId } });
    }

    [HttpPost("bazi/hepan")]
    public IActionResult HePan([FromBody] HePanRequest req)
    {
        var (a, engineA) = _charts.CalculateBazi(LabBaziMapper.ToInput(req.PersonA));
        var (b, engineB) = _charts.CalculateBazi(LabBaziMapper.ToInput(req.PersonB));
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
        var (chart, engineId) = _charts.CalculateLiuyao("coin", DateTimeOffset.Now, seed);
        return Ok(new { chart, engine = new { paipan = engineId } });
    }

    [HttpPost("liuyao/time")]
    public IActionResult LiuyaoTime([FromQuery] DateTime? at)
    {
        var when = at.HasValue ? new DateTimeOffset(at.Value) : DateTimeOffset.Now;
        var (chart, engineId) = _charts.CalculateLiuyao("time", when, null);
        return Ok(new { chart, engine = new { paipan = engineId } });
    }

    [HttpPost("liuyao/read")]
    public Task<IActionResult> LiuyaoRead([FromQuery] int tier, [FromBody] LiuyaoReadRequest req) =>
        _reads.ExecuteLiuyaoRead(tier, req);

    [HttpPost("tarot/draw")]
    public IActionResult TarotDraw([FromBody] TarotDrawRequest req)
    {
        var (reading, engineId) = _charts.DrawTarotReading(req.SpreadId, req.Question, req.Seed);
        return Ok(new { reading, engine = new { paipan = engineId } });
    }

    [HttpPost("tarot/read")]
    public Task<IActionResult> TarotRead([FromQuery] int tier, [FromBody] TarotReadRequest req) =>
        _reads.ExecuteTarotRead(tier, req);

    [HttpPost("tarot/interpret")]
    public IActionResult TarotInterpret([FromBody] TarotDrawRequest req)
    {
        var (reading, engineId) = _charts.DrawTarotReading(req.SpreadId, req.Question, req.Seed);
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
        var (huangLi, engineId) = _charts.CalculateCalendar(year, month, day, sect);
        return Ok(new { day = huangLi, engine = new { paipan = engineId } });
    }
}
