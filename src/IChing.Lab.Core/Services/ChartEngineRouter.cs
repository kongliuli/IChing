using System.Text.Json;
using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Calendar;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Core.Services;

/// <summary>
/// 按 engineId 链依次调用已注册的 <see cref="IChartEngine"/>；桥接离线时自动尝试下一项。
/// </summary>
public sealed class ChartEngineRouter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IReadOnlyDictionary<string, IChartEngine> _engines;

    public ChartEngineRouter(IEnumerable<IChartEngine> engines)
    {
        // ponytail: bazi/calendar 内置引擎共用 lunar-csharp-1.6.8，须按 domain 区分
        _engines = engines.ToDictionary(e => EngineKey(e.Domain, e.EngineId), StringComparer.OrdinalIgnoreCase);
    }

    private static string EngineKey(string domain, string engineId) => $"{domain}\u001f{engineId}";

    public IReadOnlyCollection<IChartEngine> All => _engines.Values.ToList();

    /// <summary>
    /// 按 fallback 链执行排盘；全部失败时返回最后一个错误结果（若有）。
    /// </summary>
    public ChartEngineResult Calculate(
        string domain,
        IDictionary<string, object?> args,
        IReadOnlyList<string> engineChain)
    {
        object? lastResult = null;
        string? lastEngineId = null;
        EngineMetadata? lastMetadata = null;

        foreach (var engineId in engineChain)
        {
            if (!_engines.TryGetValue(EngineKey(domain, engineId), out var engine))
            {
                continue;
            }

            lastEngineId = engine.EngineId;
            lastMetadata = engine.Metadata;
            lastResult = engine.Calculate(new ChartRequest(domain, args));

            if (!IsErrorResult(lastResult))
            {
                return new ChartEngineResult(engine.EngineId, engine.Metadata, lastResult);
            }
        }

        return new ChartEngineResult(
            lastEngineId ?? engineChain.FirstOrDefault() ?? "unknown",
            lastMetadata,
            lastResult ?? new { error = "no chart engine available", domain });
    }

    /// <summary>
    /// 从配置节解析 domain 的 default + fallback 链；未配置时返回 <paramref name="builtinEngineId"/> 单项链。
    /// </summary>
    public static IReadOnlyList<string> ResolveEngineChain(
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        string domain,
        string builtinEngineId)
    {
        var chain = new List<string>();
        string? defaultId = null;
        var fallbacks = new List<string>();

        foreach (var child in configuration.GetSection("plugins:chartEngines").GetChildren())
        {
            if (!string.Equals(child["domain"], domain, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            defaultId = child["default"];
            foreach (var fb in child.GetSection("fallback").GetChildren())
            {
                var id = fb.Value;
                if (!string.IsNullOrWhiteSpace(id))
                {
                    fallbacks.Add(id);
                }
            }

            break;
        }

        if (!string.IsNullOrWhiteSpace(defaultId))
        {
            chain.Add(defaultId);
        }

        foreach (var fb in fallbacks)
        {
            if (!chain.Contains(fb, StringComparer.OrdinalIgnoreCase))
            {
                chain.Add(fb);
            }
        }

        if (chain.Count == 0)
        {
            chain.Add(builtinEngineId);
        }

        return chain;
    }

    public static bool IsErrorResult(object result)
    {
        if (result is null)
        {
            return true;
        }

        try
        {
            var json = JsonSerializer.Serialize(result, JsonOptions);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out var error)
                && error.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(error.GetString()))
            {
                return true;
            }

            if (doc.RootElement.TryGetProperty("engine", out var engine)
                && engine.TryGetProperty("ready", out var ready)
                && ready.ValueKind == JsonValueKind.False)
            {
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}

public sealed record ChartEngineResult(string EngineId, EngineMetadata? Metadata, object Result);

/// <summary>将路由结果 coerce 为 <see cref="HuangLiDay"/>。</summary>
public static class ChartResultMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static BaziChart AsBaziChart(object result, BaziInput fallbackInput)
    {
        if (result is BaziChart chart)
        {
            return chart;
        }

        if (TryDeserialize<BaziChart>(result, out var parsed) && parsed!.DayPillar is not null)
        {
            return parsed;
        }

        return BaziEngine.Calculate(fallbackInput);
    }

    public static LiuyaoNajiaResult AsLiuyaoChart(
        object result,
        string? method,
        DateTimeOffset? at,
        int? seed)
    {
        if (result is LiuyaoNajiaResult chart)
        {
            return chart;
        }

        if (TryDeserialize<LiuyaoNajiaResult>(result, out var parsed)
            && !string.IsNullOrWhiteSpace(parsed!.OriginalHexagram))
        {
            return parsed;
        }

        var when = at ?? DateTimeOffset.Now;
        return string.Equals(method, "time", StringComparison.OrdinalIgnoreCase)
            ? LiuyaoNajiaService.Time(when)
            : LiuyaoNajiaService.Coin(when, seed);
    }

    public static HuangLiDay AsCalendarDay(object result, int year, int month, int day, int sect)
    {
        if (result is HuangLiDay dayResult)
        {
            return dayResult;
        }

        if (TryDeserialize<HuangLiDay>(result, out var parsed)
            && !string.IsNullOrWhiteSpace(parsed!.Solar))
        {
            return parsed;
        }

        return HuangLiService.GetDay(year, month, day, sect);
    }

    public static TarotReading AsTarotReading(
        object result,
        string spreadId,
        string? question,
        int? seed)
    {
        if (result is TarotReading reading)
        {
            return reading;
        }

        if (TryDeserialize<TarotReading>(result, out var parsed) && parsed!.Positions.Count > 0)
        {
            return parsed;
        }

        return TarotEngine.Draw(spreadId, question, seed);
    }

    private static bool TryDeserialize<T>(object result, out T? value)
    {
        value = default;
        try
        {
            var json = result is JsonElement element
                ? element.GetRawText()
                : JsonSerializer.Serialize(result, JsonOptions);
            value = JsonSerializer.Deserialize<T>(json, JsonOptions);
            return value is not null;
        }
        catch
        {
            return false;
        }
    }
}
