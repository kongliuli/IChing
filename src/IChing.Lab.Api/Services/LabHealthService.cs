using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Api.Contracts;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Services;
using Microsoft.Extensions.Configuration;

namespace IChing.Lab.Api.Services;

public sealed class LabHealthService
{
    private readonly IEnumerable<IChartEngine> _engines;
    private readonly IEnumerable<IInferenceEngine> _inferenceEngines;
    private readonly IConfiguration _configuration;

    public LabHealthService(
        IEnumerable<IChartEngine> engines,
        IEnumerable<IInferenceEngine> inferenceEngines,
        IConfiguration configuration)
    {
        _engines = engines;
        _inferenceEngines = inferenceEngines;
        _configuration = configuration;
    }

    public IReadOnlyList<EngineHealthStatus> GetInferenceEngineHealth()
    {
        var defaultEngineId = LabChartEngineConfig.ResolveDefaultEngineId(_configuration);
        return _inferenceEngines
            .Select(e => new EngineHealthStatus(e.EngineId, e.IsReady, e.EngineId == defaultEngineId))
            .ToList();
    }

    public IReadOnlyList<ChartEngineHealthStatus> GetChartEngineHealth()
    {
        return _engines.Select(e => new ChartEngineHealthStatus(
            e.Domain,
            e.EngineId,
            ProbeChartEngineReady(e),
            e.EngineId == LabChartEngineConfig.ResolveChartEngine(_configuration, e.Domain))).ToList();
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
}
