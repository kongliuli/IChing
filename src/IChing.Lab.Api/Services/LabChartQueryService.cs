using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Calendar;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Services;
using IChing.Lab.Core.Tarot;
using IChing.Lab.Engines.Tarot;
using Microsoft.Extensions.Configuration;

namespace IChing.Lab.Api.Services;

public sealed class LabChartQueryService
{
    private readonly ChartEngineRouter _chartRouter;
    private readonly IConfiguration _configuration;

    public LabChartQueryService(ChartEngineRouter chartRouter, IConfiguration configuration)
    {
        _chartRouter = chartRouter;
        _configuration = configuration;
    }

    public (BaziChart Chart, string EngineId) CalculateBazi(BaziInput input)
    {
        var chain = ChartEngineRouter.ResolveEngineChain(_configuration, "bazi", "lunar-csharp-1.6.8");
        var routed = _chartRouter.Calculate("bazi", ChartDemoHelper.ToArgs(input), chain);
        return (ChartResultMapper.AsBaziChart(routed.Result, input), routed.EngineId);
    }

    public (LiuyaoNajiaResult Chart, string EngineId) CalculateLiuyao(string? method, DateTimeOffset at, int? seed)
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

    public (HuangLiDay Day, string EngineId) CalculateCalendar(int year, int month, int day, int sect)
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

    public (TarotReading Reading, string EngineId) DrawTarotReading(string? spreadId, string? question, int? seed)
    {
        var chain = ChartEngineRouter.ResolveEngineChain(_configuration, "tarot", "iching-tarot-built-in");
        return TarotDrawPipeline.Draw(_chartRouter, chain, spreadId, question, seed);
    }
}
