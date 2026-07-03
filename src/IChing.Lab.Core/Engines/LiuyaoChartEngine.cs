using System.Text.Json;
using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Core.Liuyao;

namespace IChing.Lab.Core.Engines;

/// <summary>
/// 六爻纳甲排盘引擎包装类，将静态 LiuyaoNajiaService 包装为 IChartEngine 实现。
/// 支持 method=coin（铜钱法）/ method=time（按时间起卦）两种方式。
/// </summary>
public sealed class LiuyaoChartEngine : IChartEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Domain => "liuyao";

    public string EngineId => "iching-sixlines-2.0.3";

    public EngineMetadata Metadata { get; } = new(
        Source: "IChingLibrary.SixLines",
        Version: "2.0.3",
        AlgorithmBasis: "京房纳甲+世应+六亲+六神+16神煞",
        TemplateHint: "sixlines",
        ModuleFocus: ["najia", "shensha"]);

    /// <summary>根据 Args["method"] 选择起卦方式，委托给原静态方法 LiuyaoNajiaService.Coin / Time。</summary>
    public object Calculate(ChartRequest request)
    {
        var input = DeserializeArgs<LiuyaoInput>(request.Args);
        var at = input.At ?? DateTimeOffset.Now;
        return string.Equals(input.Method, "time", StringComparison.OrdinalIgnoreCase)
            ? LiuyaoNajiaService.Time(at)
            : LiuyaoNajiaService.Coin(at, input.Seed);
    }

    private static T DeserializeArgs<T>(IDictionary<string, object?> args)
    {
        var json = JsonSerializer.Serialize(args);
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException("无法从参数字典反序列化输入");
    }

    private sealed record LiuyaoInput(string? Method, DateTimeOffset? At, int? Seed);
}
