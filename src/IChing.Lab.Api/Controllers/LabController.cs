using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Tarot;
using IChing.Lab.Inference;
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
        var chart = BaziEngine.Calculate(new BaziInput(
            req.Year, req.Month, req.Day, req.Hour, req.Minute, req.Second,
            req.Longitude, req.Gender, req.Sect));
        return Ok(chart);
    }

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

    [HttpPost("tarot/draw")]
    public IActionResult TarotDraw([FromBody] TarotDrawRequest req) =>
        Ok(TarotEngine.Draw(req.SpreadId ?? "past-present-future", req.Question, req.Seed));

    [HttpGet("tarot/spreads")]
    public IActionResult TarotSpreads() => Ok(SpreadCatalog.List());
}

public record BaziRequest(
    int Year, int Month, int Day, int Hour,
    int Minute = 0, int Second = 0,
    double? Longitude = null,
    int? Gender = null,
    int Sect = 1);

public record InterpretRequest(object Chart, string? Focus, int? MaxTokens);

public record TarotDrawRequest(string? SpreadId, string? Question, int? Seed);
