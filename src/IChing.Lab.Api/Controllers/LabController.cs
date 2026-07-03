using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Tarot;
using Microsoft.AspNetCore.Mvc;

namespace IChing.Lab.Api.Controllers;

[ApiController]
[Route("lab")]
public class LabController : ControllerBase
{
    [HttpPost("bazi")]
    public IActionResult Bazi([FromBody] BaziRequest req)
    {
        var chart = BaziEngine.Calculate(new BaziInput(req.Year, req.Month, req.Day, req.Hour, req.Minute, req.Second));
        return Ok(chart);
    }

    [HttpPost("liuyao/coin")]
    public IActionResult LiuyaoCoin([FromQuery] int? seed) => Ok(LiuyaoEngine.CoinToss(seed));

    [HttpPost("liuyao/time")]
    public IActionResult LiuyaoTime([FromQuery] DateTime? at) => Ok(LiuyaoEngine.TimeHexagram(at ?? DateTime.Now));

    [HttpPost("tarot/draw")]
    public IActionResult TarotDraw([FromBody] TarotDrawRequest req) =>
        Ok(TarotEngine.Draw(req.SpreadId ?? "past-present-future", req.Question, req.Seed));
}

public record BaziRequest(int Year, int Month, int Day, int Hour, int Minute = 0, int Second = 0);

public record TarotDrawRequest(string? SpreadId, string? Question, int? Seed);
