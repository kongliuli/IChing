using System.Text.Json;
using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Core.Bazi;

namespace IChing.Lab.Core.Engines;

/// <summary>
/// 八字排盘引擎包装类，将静态 BaziEngine 包装为 IChartEngine 实现。
/// </summary>
public sealed class BaziChartEngine : IChartEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Domain => "bazi";

    public string EngineId => "lunar-csharp-1.6.8";

    /// <summary>从参数字典反序列化 BaziInput，委托给原静态方法 BaziEngine.Calculate 计算排盘。</summary>
    public object Calculate(ChartRequest request)
    {
        var input = DeserializeArgs<BaziInput>(request.Args);
        return BaziEngine.Calculate(input);
    }

    private static T DeserializeArgs<T>(IDictionary<string, object?> args)
    {
        var json = JsonSerializer.Serialize(args);
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException("无法从参数字典反序列化输入");
    }
}
